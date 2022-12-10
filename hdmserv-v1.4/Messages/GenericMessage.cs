using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Messages
{
    [Serializable]
   public class GenericMessage :AsyncPipes.IMessage 
    {
     #region IMessage Members
       private Guid _messageId;
       private string _originator;
       private string _recipient;
       private DateTime _messageDateTime;
       private Type _messageType;
       private byte[] _payload;

        public Guid MessageId
        {
            get
            {
                return _messageId;
            }
            set
            {
                _messageId = value;
            }
        }

        public string Originator
        {
            get
            {
                return _originator;
            }
            set
            {
                _originator = value;
            }
        }

        public string Recipient
        {
            get
            {
                return _recipient;
            }
            set
            {
                _recipient = value;
            }
        }

        public DateTime MessageDateTime
        {
            get
            {
                return _messageDateTime;
            }
            set
            {
                _messageDateTime = value;
            }
        }

        public Type MessageType
        {
            get
            {
                return _messageType;
            }
            set
            {
                _messageType = value;
            }
        }

        public byte[] Payload
        {
            get
            {
                return _payload;
            }
            set
            {
                _payload = value;
            }
        }

      #endregion

        public GenericMessage()
        {
        }

        public GenericMessage(Guid messageId,string originator,string recipient,DateTime messageDateTime, Type messageType,byte[] payload )
        {
            this._messageId = messageId;
            this._originator = originator;
            this._recipient = recipient;
            this._messageDateTime = messageDateTime;
            this._messageType = messageType;
            this._payload = payload;
        }
    }
}
