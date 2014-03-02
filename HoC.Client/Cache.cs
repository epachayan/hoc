//-----------------------------------------------------------------------
// <copyright file="Cache.cs">
//     Copyright (c) Nitin Stephen Koshy. All rights reserved.
// </copyright>
// <author>Nitin Stephen Koshy</author>
// <email>nitinkoshy@gmail.com</email>
// <date>12.Sep.2010</date>
// <license>Microsoft Public License (http://www.microsoft.com/opensource/licenses.mspx#Ms-PL)</license>
// <Url>http://hoc.codeplex.com</Url>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using HoC.Client.CacheService;
using System.ServiceModel;
using System.Xml.Serialization;
using System.IO;
using System.IO.Compression;
using HoC.Common;
using System.Configuration;

namespace HoC.Client
{
    //the entry point for the client applications to talk
    //this version is not thread safe
    public class Cache
    {
        private Dictionary<string, CacheServiceClient> dictionaryList = new Dictionary<string, CacheServiceClient>();
        private HashNodeResolver _nodeResolver = new HashNodeResolver();
        private bool _useCompression;
        private const int _dataSizeLimit = 1048576; //1MB default
        private ObjectInstancePool<MemoryStream> memPool = new ObjectInstancePool<MemoryStream>(); //mem stream creation is time consuming, hence pool

        public Cache()
        {
            _useCompression = Convert.ToBoolean(ConfigurationManager.AppSettings["UseCompression"] ?? "false");
        }

        public object this[string key]
        {
            get
            {
                try
                {
                    ClientCacheItem cacheItem = GetCacheServiceClient(key).RetrieveCacheItem(key);
                    if (cacheItem == null)
                        return null;

                    byte[] dataArray = cacheItem.Value;
                    if (_useCompression)
                    {
                        dataArray = DeCompress(cacheItem.Value);
                    }
                    return DeSerialize(dataArray, cacheItem.TypeAsString);
                }
                catch
                {
                    return null; //return null in case of any connection error
                }
            }

            set
            {
                MemoryStream dataStream = Serialize(value);
                byte[] objectAsBytes = dataStream.ToArray();
                if (_useCompression)
                {
                    objectAsBytes = Compress(dataStream);

                }
                ClientCacheItem cacheItem = new ClientCacheItem()
                {
                    TypeAsString = value.GetType().AssemblyQualifiedName,
                    Value = objectAsBytes
                };
                GetCacheServiceClient(key).StoreCacheItem(key, cacheItem);
            }
        }

        private byte[] Compress(Stream memStream)
        {
            MemoryStream memStreamCompressed = memPool.GetInstance();
            GZipStream deflateStream = new GZipStream(memStreamCompressed, CompressionMode.Compress, true);
            memStream.Position = 0;
            memStream.CopyTo(deflateStream);
            deflateStream.Close();

            return memStreamCompressed.ToArray();
        }

        private byte[] DeCompress(byte[] cacheItem)
        {
            MemoryStream inputStream = new MemoryStream(cacheItem);
            inputStream.Position = 0;
            MemoryStream memStreamUncompressed = memPool.GetInstance();
            GZipStream deflateStream = new GZipStream(inputStream, CompressionMode.Decompress);
            memStreamUncompressed.Position = 0;
            deflateStream.CopyTo(memStreamUncompressed);
            deflateStream.Close();
            return memStreamUncompressed.ToArray();
        }

        private MemoryStream Serialize(Object value)
        {
            XmlSerializer serializer = new XmlSerializer(value.GetType());
            MemoryStream memStream = memPool.GetInstance();

            serializer.Serialize(memStream, value);
            if (memStream.Length > _dataSizeLimit)
                throw new ObjectSizeTooLargeToCacheException(String.Format("Max Object size allowed : {0} bytes", _dataSizeLimit));

            memStream.Position = 0;
            return memStream;

        }

        private Object DeSerialize(byte[] value, string typeAsString)
        {
            MemoryStream inputStream = new MemoryStream(value);
            inputStream.Position = 0;
            XmlSerializer serializer = new XmlSerializer(Type.GetType(typeAsString));
            Object item = serializer.Deserialize(inputStream);
            return item;
        }

        private CacheServiceClient GetCacheServiceClient(string key)
        {
            Node node = _nodeResolver.GetObjectLocation(key);
            CacheServiceClient serviceClient;

            if (!(dictionaryList.TryGetValue(node.EndPoint.ToString(), out serviceClient)))
            {
                string endPoint = string.Format("net.tcp://{0}:{1}/HoCCacheService", node.EndPoint.ToString(), node.ServicePort);
                serviceClient = new CacheServiceClient(new NetTcpBinding(SecurityMode.None), new EndpointAddress(endPoint));
                //serviceClient = new CacheServiceClient(new NetTcpBinding(), new EndpointAddress(endPoint));
                serviceClient.Open();
                dictionaryList.Add(node.EndPoint.ToString(), serviceClient);
            }

            return serviceClient;
        }
    }

    class ObjectSizeTooLargeToCacheException : ApplicationException
    {
        private string message;

        public ObjectSizeTooLargeToCacheException(string errorMessage)
        {
            message = errorMessage;
        }

        private ObjectSizeTooLargeToCacheException()
        {

        }

    }
}