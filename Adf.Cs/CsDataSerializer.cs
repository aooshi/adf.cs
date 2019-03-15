using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections;

namespace Adf.Cs
{
    /// <summary>
    /// 数据序列化工具
    /// </summary>
    public sealed class CsDataSerializer : Adf.DataSerializable
    {
        const int INT32_SIZE = sizeof(Int32);

        /// <summary>
        /// 数据实例化默认实例
        /// </summary>
        public static CsDataSerializer Instance = new CsDataSerializer();
        
        private static readonly Type TYPE_ILIST = typeof(IList);
        /// <summary>
        /// 重写将字节数组转换为对象函数，以ProtoBuf替换原有
        /// </summary>
        /// <param name="type"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        protected override object DeserializeObject(Type type, byte[] buffer)
        {
            //添加列表，以支持泛型列表的序列化， 系统虽支持但不建议使用列表，应使用数组
            if (type.IsGenericType && TYPE_ILIST.IsAssignableFrom(type))
                return this.DeserializeList(type, buffer);

            using (var m = new MemoryStream())
            {
                m.Write(buffer, 0, buffer.Length);
                m.Position = 0;
                return ProtoBuf.Serializer.NonGeneric.Deserialize(type, m);
            }
        }

        /// <summary>
        /// 重写序列化对象为字节数组孙数，以ProtoBuf替换原有
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected override byte[] SerializeObject(object value)
        {
            if (value is IList)
            {
                return this.SerializeList(value);
            }

            using (var m = new MemoryStream())
            {
                ProtoBuf.Serializer.NonGeneric.Serialize(m, value);
                return m.ToArray();
            }
        }

        /// <summary>
        /// 解析指定类型数组对象
        /// </summary>
        /// <param name="type"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private object DeserializeList(Type type, byte[] buffer)
        {
            //ex:
            //if (listType.IsGenericType && typeof(IList).IsAssignableFrom(listType))
            //{
            //    var test = (IList)Activator.CreateInstance(listType);
            //    var b = listType.GetGenericArguments()[0];
            //}

            //legnth
            var arrayLength = BaseDataConverter.ToInt32(buffer);
            var offset = INT32_SIZE;

            //item length,  item size1, item 1, item size 2, item 2, item size N, item N

            var workArray = (IList)Activator.CreateInstance(type,arrayLength);
            var elementType = type.GetGenericArguments()[0];
            int itemSize;
            byte[] itemBuffer;
            for (int i = 0; i < arrayLength; i++)
            {
                itemSize = BaseDataConverter.ToInt32(buffer, offset);
                offset += INT32_SIZE;

                if (itemSize == -1)
                {
                    workArray.Add(null);
                }
                else
                {
                    itemBuffer = new byte[itemSize];
                    Array.Copy(buffer, offset, itemBuffer, 0, itemSize);
                    workArray.Add(this.Deserialize(elementType, itemBuffer));
                    offset += itemSize;
                }
            }
            return workArray;
        }

        /// <summary>
        /// SerializeArray
        /// </summary>
        /// <param name="list"></param>
        private byte[] SerializeList(object list)
        {
            byte[] itemBuffer, lengthBuffer, sizeBuffer;
            var workArray = (IList)list;
            var arrayLength = workArray.Count;
            int length;
            using (var m = new MemoryStream())
            {
                //write length
                lengthBuffer = BaseDataConverter.ToBytes(arrayLength);
                m.Write(lengthBuffer, 0, INT32_SIZE);

                //item length,  item size1, item 1, item size 2, item 2, item size N, item N
                foreach (var item in workArray)
                {
                    itemBuffer = Serialize(item);
                    if (itemBuffer == null)
                    {
                        sizeBuffer = BaseDataConverter.ToBytes(-1);
                        //write size
                        m.Write(sizeBuffer, 0, INT32_SIZE);
                        //null item no body
                    }
                    else
                    {
                        length = itemBuffer.Length;
                        //write size
                        sizeBuffer = BaseDataConverter.ToBytes(length);
                        m.Write(sizeBuffer, 0, INT32_SIZE);
                        //item
                        m.Write(itemBuffer, 0, length);
                    }
                }
                return m.ToArray();
            }
        }
    }
}
