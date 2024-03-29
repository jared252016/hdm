﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace AsyncPipes
{
   public static class MessageSerializers
    {
       public static byte[] SerializeMessage(IMessage message)
       {
           BinaryFormatter formatter = new BinaryFormatter();
           MemoryStream ms = new MemoryStream();
           formatter.Serialize(ms, message);
           return ms.ToArray();
       }

       public static IMessage DeserializeMessage(byte[]  bMessage)
       {
           BinaryFormatter formatter = new BinaryFormatter();
           MemoryStream ms = new MemoryStream(bMessage);
           Object message= formatter.Deserialize(ms);
           return (IMessage) message;
       }

       public static byte[] SerializeObject( object item)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        MemoryStream ms = new MemoryStream();
        formatter.Serialize(ms, item);
        return ms.ToArray();
    }

       public static object DeserializeObject(byte[] bObject)
       {
           BinaryFormatter formatter = new BinaryFormatter();
           MemoryStream ms = new MemoryStream(bObject);
           Object message = formatter.Deserialize(ms);
           return message;
       }
    }
}
