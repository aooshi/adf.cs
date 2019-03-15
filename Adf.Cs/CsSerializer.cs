using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections;

namespace Adf.Cs
{
    /// <summary>
    /// Serializer(AppSetting:CsBinarySerialize)
    /// </summary>
    public class CsSerializer
    {
        static readonly bool isBinary = ConfigHelper.GetSettingAsBoolean("CsBinarySerialize", false);

        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte[] Serialize(object value)
        {
            using (var m = new MemoryStream())
            {
                Serialize(m, value);
                return m.ToArray();
            }
        }

        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="target"></param>
        /// <param name="obj"></param>
        public static void Serialize(Stream target, object obj)
        {
            if (obj != null)
            {
                if (!isBinary)
                {
                    ProtoBuf.Serializer.NonGeneric.Serialize(target, obj);
                }
                else
                {
                    if (ProtoBuf.Serializer.NonGeneric.CanSerialize(obj.GetType()))
                    {
                        ProtoBuf.Serializer.NonGeneric.Serialize(target, obj);
                    }
                    else
                    {
                        var formatter = new BinaryFormatter();
                        formatter.Serialize(target, obj);
                    }
                }
            }
        }
        
        /// <summary>
        /// 返序列化
        /// </summary>
        /// <param name="type"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static object Deserialize(Type type, Stream stream)
        {
            if (!isBinary)
                return ProtoBuf.Serializer.NonGeneric.Deserialize(type, stream);

            if (ProtoBuf.Serializer.NonGeneric.CanSerialize(type))
            {
                return ProtoBuf.Serializer.NonGeneric.Deserialize(type, stream);
            }
            else
            {
                var formatter = new BinaryFormatter();
                return formatter.Deserialize(stream);
            }
        }
    }
}
