//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.3031
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;
using WCell.Util;

//
// This source code was auto-generated by xsd, Version=2.0.50727.1432.
//

namespace WCell.PacketAnalysis.Logs
{
    /// <remarks/>
    [GeneratedCode("xsd", "2.0.50727.1432")]
    [Serializable]
    [DebuggerStepThrough]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot("sniffitztlog", Namespace = "", IsNullable = false)]
    public class SniffitztLog : XmlFile<SniffitztLog>
    {
        /// <remarks/>
        [XmlElement("header")]
        public SniffitztlogHeader Header;

        /// <remarks/>
        [XmlElement("packet")]
        public SniffitztlogPacket[] Packets;
    }

    /// <remarks/>
    [GeneratedCode("xsd", "2.0.50727.1432")]
    [Serializable]
    [DebuggerStepThrough]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class SniffitztlogHeader
    {
        /// <remarks/>
        [XmlAttribute("accountName")]
        public string AccountName;

        /// <remarks/>
        [XmlAttribute("clientBuild")]
        public ushort ClientBuild;

        /// <remarks/>
        [XmlAttribute("clientLang")]
        public string ClientLang;

        /// <remarks/>
        [XmlAttribute("realmName")]
        public string RealmName;

        /// <remarks/>
        [XmlAttribute("realmServer")]
        public string RealmAddress;

        /// <remarks/>
        [XmlAttribute("snifferVersion")]
        public string Version;
    }

    /// <remarks/>
    [GeneratedCode("xsd", "2.0.50727.1432")]
    [Serializable]
    [DebuggerStepThrough]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class SniffitztlogPacket
    {
        /// <remarks/>
        [XmlAttribute("date")]
        public uint Date;

        /// <remarks/>
        [XmlAttribute("direction")]
        public SniffitztDirection Direction;

        /// <remarks/>
        [XmlAttribute("opcode")]
        public ushort Opcode;

        /// <remarks/>
        [XmlText]
        public string Value;

        public PacketSender Sender
        {
            get
            {
                return (PacketSender)Direction;
            }
        }
    }

    public enum SniffitztDirection
    {
        /// <summary>
        /// Server
        /// </summary>
        S2C = 1,
        /// <summary>
        /// Client
        /// </summary>
        C2S = 2
    }
}