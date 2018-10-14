﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using NewLife.Net;
using NewLife.RocketMQ.Client;
using NewLife.RocketMQ.Protocol;

namespace NewLife.RocketMQ
{
    class MQClient : DisposeBase
    {
        #region 属性
        public String Id { get; }

        public MQAdmin Config { get; }

        private TcpClient _Client;
        private Stream _Stream;
        #endregion

        #region 构造
        public MQClient(String id, MQAdmin config)
        {
            Id = id;
            Config = config;
        }
        #endregion

        #region 方法
        public void Start()
        {
            var cfg = Config;
            var ss = cfg.NameServerAddress.Split(";");
            var uri = new NetUri(ss[0]);

            var client = new TcpClient();
            //client.Connect(uri.EndPoint);

            var timeout = 3_000;
            // 采用异步来解决连接超时设置问题
            var ar = client.BeginConnect(uri.Address, uri.Port, null, null);
            if (!ar.AsyncWaitHandle.WaitOne(timeout, false))
            {
                client.Close();
                throw new TimeoutException($"连接[{uri}][{timeout}ms]超时！");
            }

            _Client = client;

            _Stream = new BufferedStream(client.GetStream());
        }

        private Int32 g_id;
        public Command Send(Command cmd)
        {
            if (cmd.Header.Opaque == 0) cmd.Header.Opaque = g_id++;

            cmd.Write(_Stream);
            //var ms = new MemoryStream();
            //cmd.Write(ms);
            //XTrace.WriteLine(ms.ToArray().ToHex());

            var rs = new Command();
            rs.Read(_Stream);

            return rs;
        }

        public Command Send(RequestCode request, Object extFields = null)
        {
            var header = new Header
            {
                Code = (Int32)request,
            };

            var cmd = new Command
            {
                Header = header,
            };

            if (extFields != null) header.ExtFields = extFields.ToDictionary().ToDictionary(e => e.Key, e => e.Value + "");

            return Send(cmd);
        }
        #endregion

        #region 命令
        public Command GetRouteInfo(String topic)
        {
            //var header = new Header
            //{
            //    Code = (Int32)RequestCode.GET_ROUTEINTO_BY_TOPIC,
            //};

            //var cmd = new Command
            //{
            //    Header = header,
            //};

            return Send(RequestCode.GET_ROUTEINTO_BY_TOPIC, new { topic });
        }
        #endregion
    }
}