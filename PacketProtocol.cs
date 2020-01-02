using System;

namespace Assets.RemoteHandsTracking
{
    // Original source: https://blog.stephencleary.com/2009/04/sample-code-length-prefix-message.html
    /// <summary>
    /// Maintains the necessary buffers for applying a length-prefix message framing protocol over a stream.
    /// </summary>
    /// <remarks>
    /// <para>Create one instance of this class for each incoming stream, and assign a handler to <see cref="MessageArrived"/>. As bytes arrive at the stream, pass them to <see cref="DataReceived"/>, which will invoke <see cref="MessageArrived"/> as necessary.</para>
    /// <para>If <see cref="DataReceived"/> raises <see cref="System.Net.ProtocolViolationException"/>, then the stream data should be considered invalid. After that point, no methods should be called on that <see cref="PacketProtocol"/> instance.</para>
    /// <para>This class uses a 4-byte signed integer length prefix, which allows for message sizes up to 2 GB. Keepalive messages are supported as messages with a length prefix of 0 and no message data.</para>
    /// <para>This is EXAMPLE CODE! It is not particularly efficient; in particular, if this class is rewritten so that a particular interface is used (e.g., Socket's IAsyncResult methods), some buffer copies become unnecessary and may be removed.</para>
    /// </remarks>
    public class PacketProtocol
    {
        /// <summary>
        /// Wraps a message. The wrapped message is ready to send to a stream.
        /// </summary>
        /// <remarks>
        /// <para>Generates a length prefix for the message and returns the combined length prefix and message.</para>
        /// </remarks>
        /// <param name="message">The message to send.</param>
        public static byte[] WrapMessage(byte[] message)
        {
            // Get the length prefix for the message
            byte[] lengthPrefix = BitConverter.GetBytes(message.Length);

            // Concatenate the length prefix and the message
            byte[] ret = new byte[lengthPrefix.Length + message.Length];
            lengthPrefix.CopyTo(ret, 0);
            message.CopyTo(ret, lengthPrefix.Length);

            return ret;
        }

        /// <summary>
        /// Wraps a keepalive (0-length) message. The wrapped message is ready to send to a stream.
        /// </summary>
        public static byte[] WrapKeepaliveMessage()
        {
            return BitConverter.GetBytes((int)0);
        }

        /// <summary>
        /// Initializes a new <see cref="PacketProtocol"/>, limiting message sizes to the given maximum size.
        /// </summary>
        /// <param name="maxMessageSize">The maximum message size supported by this protocol. This may be less than or equal to zero to indicate no maximum message size.</param>
        public PacketProtocol(int maxMessageSize, Action<byte[]> messageArrived)
        {
            // We allocate the buffer for receiving message lengths immediately
            this.lengthBuffer = new byte[sizeof(int)];
            this.maxMessageSize = maxMessageSize;
            MessageArrived = messageArrived;
        }

        /// <summary>
        /// The buffer for the length prefix; this is always 4 bytes long.
        /// </summary>
        private byte[] lengthBuffer;

        /// <summary>
        /// The buffer for the data; this is null if we are receiving the length prefix buffer.
        /// </summary>
        private byte[] dataBuffer;

        /// <summary>
        /// The number of bytes already read into the buffer (the length buffer if <see cref="dataBuffer"/> is null, otherwise the data buffer).
        /// </summary>
        private int bytesReceived;

        /// <summary>
        /// The maximum size of messages allowed.
        /// </summary>
        private int maxMessageSize;

        /// <summary>
        /// Indicates the completion of a message read from the stream.
        /// </summary>
        /// <remarks>
        /// <para>This may be called with an empty message, indicating that the other end had sent a keepalive message. This will never be called with a null message.</para>
        /// <para>This event is invoked from within a call to <see cref="DataReceived"/>. Handlers for this event should not call <see cref="DataReceived"/>.</para>
        /// </remarks>
        public Action<byte[]> MessageArrived { get; }

        /// <summary>
        /// Notifies the <see cref="PacketProtocol"/> instance that incoming data has been received from the stream. This method will invoke <see cref="MessageArrived"/> as necessary.
        /// </summary>
        /// <remarks>
        /// <para>This method may invoke <see cref="MessageArrived"/> zero or more times.</para>
        /// <para>Zero-length receives are ignored. Many streams use a 0-length read to indicate the end of a stream, but <see cref="PacketProtocol"/> takes no action in this case.</para>
        /// </remarks>
        /// <param name="data">The data received from the stream. Cannot be null.</param>
        /// <exception cref="System.Net.ProtocolViolationException">If the data received is not a properly-formed message.</exception>
        public void DataReceived(byte[] data)
        {
            // Process the incoming data in chunks, as the ReadCompleted requests it

            // Logically, we are satisfying read requests with the received data, instead of processing the
            //  incoming buffer looking for messages.

            int i = 0;
            while (i != data.Length)
            {
                // Determine how many bytes we want to transfer to the buffer and transfer them
                int bytesAvailable = data.Length - i;
                if (this.dataBuffer != null)
                {
                    // We're reading into the data buffer
                    int bytesRequested = this.dataBuffer.Length - this.bytesReceived;

                    // Copy the incoming bytes into the buffer
                    int bytesTransferred = Math.Min(bytesRequested, bytesAvailable);
                    Array.Copy(data, i, this.dataBuffer, this.bytesReceived, bytesTransferred);
                    i += bytesTransferred;

                    // Notify "read completion"
                    this.ReadCompleted(bytesTransferred);
                }
                else
                {
                    // We're reading into the length prefix buffer
                    int bytesRequested = this.lengthBuffer.Length - this.bytesReceived;

                    // Copy the incoming bytes into the buffer
                    int bytesTransferred = Math.Min(bytesRequested, bytesAvailable);
                    Array.Copy(data, i, this.lengthBuffer, this.bytesReceived, bytesTransferred);
                    i += bytesTransferred;

                    // Notify "read completion"
                    this.ReadCompleted(bytesTransferred);
                }
            }
        }

        /// <summary>
        /// Called when a read completes. Parses the received data and calls <see cref="MessageArrived"/> if necessary.
        /// </summary>
        /// <param name="count">The number of bytes read.</param>
        /// <exception cref="System.Net.ProtocolViolationException">If the data received is not a properly-formed message.</exception>
        private void ReadCompleted(int count)
        {
            // Get the number of bytes read into the buffer
            this.bytesReceived += count;

            if (this.dataBuffer == null)
            {
                // We're currently receiving the length buffer

                if (this.bytesReceived != sizeof(int))
                {
                    // We haven't gotten all the length buffer yet: just wait for more data to arrive
                }
                else
                {
                    // We've gotten the length buffer
                    int length = BitConverter.ToInt32(this.lengthBuffer, 0);

                    // Sanity check for length < 0
                    if (length < 0)
                        throw new System.Net.ProtocolViolationException("Message length is less than zero");

                    // Another sanity check is needed here for very large packets, to prevent denial-of-service attacks
                    if (this.maxMessageSize > 0 && length > this.maxMessageSize)
                        throw new System.Net.ProtocolViolationException("Message length " + length.ToString(System.Globalization.CultureInfo.InvariantCulture) + " is larger than maximum message size " + this.maxMessageSize.ToString(System.Globalization.CultureInfo.InvariantCulture));

                    // Zero-length packets are allowed as keepalives
                    if (length == 0)
                    {
                        this.bytesReceived = 0;
                        if (this.MessageArrived != null)
                            this.MessageArrived(new byte[0]);
                    }
                    else
                    {
                        // Create the data buffer and start reading into it
                        this.bytesReceived = 0;
                        this.dataBuffer = new byte[length];
                    }
                }
            }
            else
            {
                if (this.bytesReceived != this.dataBuffer.Length)
                {
                    // We haven't gotten all the data buffer yet: just wait for more data to arrive
                }
                else
                {
                    // We've gotten an entire packet
                    if (this.MessageArrived != null)
                        this.MessageArrived(this.dataBuffer);

                    // Start reading the length buffer again
                    this.dataBuffer = null;
                    this.bytesReceived = 0;
                }
            }
        }
    }
}