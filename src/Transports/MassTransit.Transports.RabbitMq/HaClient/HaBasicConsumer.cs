﻿// Copyright 2007-2012 Chris Patterson, Dru Sellers, Travis Smith, et. al.
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
namespace MassTransit.Transports.RabbitMq.HaClient
{
    using System;
    using System.Collections.Generic;
    using Logging;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;


    public class HaBasicConsumer :
        IHaBasicConsumer
    {
        static readonly ILog _log = Logger.Get<HaBasicConsumer>();
        readonly IDictionary<string, object> _arguments;
        readonly IBasicConsumer _consumer;
        readonly IBasicConsumerEventSink _eventSink;
        readonly bool _exclusive;
        readonly bool _noAck;
        readonly bool _noLocal;
        readonly string _queue;
        string _consumerTag;
        bool _ok;

        public HaBasicConsumer(IBasicConsumerEventSink eventSink, string queue, bool noAck, string consumerTag,
            bool noLocal, bool exclusive, IDictionary<string, object> arguments, IBasicConsumer consumer)
        {
            _eventSink = eventSink;
            _queue = queue;
            _noAck = noAck;
            _consumerTag = consumerTag;
            _noLocal = noLocal;
            _exclusive = exclusive;
            _arguments = arguments;
            _consumer = consumer;
        }

        public IDictionary<string, object> Arguments
        {
            get { return _arguments; }
        }

        public bool Exclusive
        {
            get { return _exclusive; }
        }

        public bool NoAck
        {
            get { return _noAck; }
        }

        public bool NoLocal
        {
            get { return _noLocal; }
        }

        public string Queue
        {
            get { return _queue; }
        }

        public string ConsumerTag
        {
            get { return _consumerTag; }
        }

        public void HandleModelDisposed(IModel model, ShutdownEventArgs reason)
        {
            if (_log.IsDebugEnabled)
                _log.DebugFormat("ModelDisposed on consumer {0} {1}: {2}", _consumerTag, reason.ReplyCode,
                    reason.ReplyText);

            _consumer.HandleModelShutdown(model, reason);
        }

        public void HandleBasicConsumeOk(string consumerTag)
        {
            if (_log.IsDebugEnabled)
                _log.DebugFormat("BasicConsumer: {0} - Ok", consumerTag);

            _consumerTag = consumerTag;

            if (_ok)
                return;

            _consumer.HandleBasicConsumeOk(consumerTag);

            _eventSink.RegisterConsumer(consumerTag, this);

            _ok = true;
        }

        public void HandleBasicCancelOk(string consumerTag)
        {
            if (_log.IsDebugEnabled)
                _log.DebugFormat("BasicCancelOk: {0} - {1}", consumerTag, _consumerTag);

            _consumer.HandleBasicCancelOk(consumerTag);

            _eventSink.CancelConsumer(consumerTag);
        }

        public void HandleBasicCancel(string consumerTag)
        {
            if (_log.IsDebugEnabled)
                _log.DebugFormat("BasicCancel: {0} - {1}", consumerTag, _consumerTag);

            _consumer.HandleBasicCancel(consumerTag);
        }

        public void HandleModelShutdown(IModel model, ShutdownEventArgs reason)
        {
            if (_log.IsDebugEnabled)
                _log.DebugFormat("ModelShutdown on consumer {0} {1}: {2}", _consumerTag, reason.ReplyCode,
                    reason.ReplyText);
        }

        public void HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered, string exchange,
            string routingKey, IBasicProperties properties, byte[] body)
        {
            try
            {
                if (_log.IsDebugEnabled)
                    _log.DebugFormat("HandleBasicDeliver: {0} - {1}", consumerTag, deliveryTag);

                _eventSink.DeliveryReceived(consumerTag, deliveryTag, redelivered, exchange, routingKey);

                _consumer.HandleBasicDeliver(consumerTag, deliveryTag, redelivered, exchange, routingKey, properties,
                    body);

                _eventSink.DeliveryCompleted(consumerTag, deliveryTag);
            }
            catch (Exception ex)
            {
                _eventSink.DeliveryFaulted(consumerTag, deliveryTag, ex);

                throw;
            }
        }

        public IModel Model
        {
            get { return _consumer.Model; }
        }

        public event ConsumerCancelledEventHandler ConsumerCancelled
        {
            add { _consumer.ConsumerCancelled += value; }
            remove { _consumer.ConsumerCancelled -= value; }
        }
    }
}