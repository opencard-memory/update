using BSL.v41.Logic.Environment.LaserMessage.Sepo.Alliance;
using BSL.v41.Supercell.Titan.CommonUtils;
namespace BSL.v41.Logic.Environment.LaserMessage.Laser.Node_11;
public sealed class JoinableAllianceListMessage : PiranhaMessage
{
 private IReadOnlyList<AllianceHeaderEntry> _entries = Array.Empty<AllianceHeaderEntry>();
 public JoinableAllianceListMessage(){Helper.Skip();}
 public JoinableAllianceListMessage(IReadOnlyList<AllianceHeaderEntry> entries){_entries=entries;}
 public override void Encode(){base.Encode();ByteStream.WriteVInt(_entries.Count);foreach(var e in _entries)e.Encode(ByteStream);}
 public override int GetMessageType()=>24304;
 public override int GetServiceNodeType()=>11;
}
