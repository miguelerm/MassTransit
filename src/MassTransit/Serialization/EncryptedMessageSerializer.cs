﻿// Copyright 2007-2015 Chris Patterson, Dru Sellers, Travis Smith, et. al.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace MassTransit.Serialization
{
    using System.IO;
    using System.Net.Mime;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Bson;


    public class EncryptedMessageSerializer :
        IMessageSerializer
    {
        public const string ContentTypeHeaderValue = "application/vnd.masstransit+aes";
        public static readonly ContentType EncryptedContentType = new ContentType(ContentTypeHeaderValue);
        public const string EncryptionKeyHeader = "EncryptionKeyId";
        readonly JsonSerializer _serializer;
        readonly ICryptoStreamProvider _streamProvider;

        public EncryptedMessageSerializer(ICryptoStreamProvider streamProvider)
        {
            _streamProvider = streamProvider;
            _serializer = BsonMessageSerializer.Serializer;
        }

        public ContentType ContentType
        {
            get { return EncryptedContentType; }
        }

        public void Serialize<T>(Stream stream, SendContext<T> context) where T : class
        {
            context.ContentType = EncryptedContentType;

            var envelope = new JsonMessageEnvelope(context, context.Message, typeof(T).GetMessageTypes());

            using (Stream cryptoStream = _streamProvider.GetEncryptStream(stream, context))
            using (var jsonWriter = new BsonWriter(cryptoStream))
            {
                _serializer.Serialize(jsonWriter, envelope, typeof(MessageEnvelope));

                jsonWriter.Flush();
            }
        }
    }
}