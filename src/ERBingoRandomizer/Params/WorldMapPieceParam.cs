using System;
using FSParam;

namespace Project.Params
{
    public class WorldMapPieceParam
    {
        private Param.Cell _openEventFlagId;

        public WorldMapPieceParam(Param.Row lot)
        {
            _openEventFlagId = lot["openEventFlagId"]!.Value;
        }

        public uint openEventFlagId { get => (uint)_openEventFlagId.Value; set => _openEventFlagId.Value = value; }

        public override int GetHashCode() { return openEventFlagId.GetHashCode(); }

    }
}
