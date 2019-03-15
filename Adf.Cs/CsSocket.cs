using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net.Sockets;

namespace Adf.Cs
{
    /// <summary>
    /// Socket Defined
    /// </summary>
    public static class CsSocket
    {
        /// <summary>
        /// DataType Buffer Size
        /// </summary>
        public const int DATATYPE_BUFFER_SIZE = 1;
        /// <summary>
        /// Length Buffer Size
        /// </summary>
        public const int LENGTH_BUFFER_SIZE = 4;
        /// <summary>
        /// Command Buffer Size
        /// </summary>
        public const int COMMAND_BUFFER_SIZE = 128;
        /// <summary>
        /// Body Buffer Size
        /// </summary>
        public const int BODY_BUFFER_SIZE = 512;

        /// <summary>
        /// 向流中写入数据项
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="data"></param>
        public static void WriteDataItem(Stream stream, object data)
        {
            byte[] buffer = CsDataSerializer.Instance.Serialize(data);
            byte[] sizeBuffer = null;

            //length1, data1,length2, data2,lengthN, dataN

            //write length
            if (buffer == null)
            {
                sizeBuffer = BaseDataConverter.ToBytes(-1);
                stream.Write(sizeBuffer, 0, LENGTH_BUFFER_SIZE);
                //not nody
            }
            else
            {
                int length = buffer.Length;
                sizeBuffer = BaseDataConverter.ToBytes(length);
                stream.Write(sizeBuffer, 0, LENGTH_BUFFER_SIZE);
                stream.Write(buffer, 0, length);
            }
        }

        /// <summary>
        /// 读取指定类型数据
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object Receive(Socket socket, Type type)
        {
            //length1, data1,length2, data2,lengthN, dataN
            var length = BaseDataConverter.ToInt32(SocketHelper.Receive(socket, LENGTH_BUFFER_SIZE));
            if (length == -1)
            {
                return null;
            }

            var buffer = SocketHelper.Receive(socket, length);
            return CsDataSerializer.Instance.Deserialize(type, buffer);
        }

        /// <summary>
        /// 读取一个数据并将其抛弃
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        public static void Receive(Socket socket)
        {
            //length1, data1,length2, data2,lengthN, dataN
            var length = BaseDataConverter.ToInt32(SocketHelper.Receive(socket, LENGTH_BUFFER_SIZE));
            if (length == -1)
            {
            }
            else
            {
                var buffer = SocketHelper.Receive(socket, length);
            }
        }
    }
}