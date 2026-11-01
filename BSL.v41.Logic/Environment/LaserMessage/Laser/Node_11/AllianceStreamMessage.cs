using BSL.v41.Supercell.Titan.CommonUtils;
namespace BSL.v41.Logic.Environment.LaserMessage.Laser.Node_11;
public sealed class AllianceStreamMessage : PiranhaMessage
{
 private readonly long _streamId; private readonly long _accountId; private readonly string _name; private readonly string _text; private readonly int _timestamp;
 public AllianceStreamMessage(){Helper.Skip();}
 public AllianceStreamMessage(long streamId,long accountId,string name,string text,int timestamp){_streamId=streamId;_accountId=accountId;_name=name;_text=text;_timestamp=timestamp;}
 public override void Encode(){base.Encode();ByteStream.WriteVInt(1);ByteStream.WriteLong(_streamId);ByteStream.WriteVInt(2);ByteStream.WriteLong(_accountId);ByteStream.WriteString(_name);ByteStream.WriteString(_text);ByteStream.WriteVInt(_timestamp);}
 public override int GetMessageType()=>24311;
 public override int GetServiceNodeType()=>11;
}
