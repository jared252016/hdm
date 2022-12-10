using System;

namespace AsyncPipes
{
    /// <summary>
    /// Interface for Async Pipes Message objects.
    /// </summary>
    public interface IMessage
    {
        Guid MessageId { get; set; }
        String Originator { get; set; }
        String Recipient { get; set;}
        DateTime MessageDateTime { get; set; }
        Type MessageType { get; set;}
        byte[] Payload { get; set; }
    }
}