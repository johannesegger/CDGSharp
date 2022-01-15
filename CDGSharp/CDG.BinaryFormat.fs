module CDG.BinaryFormat

module CDGPacketInstruction =
    let dataLength = 16

module SubCodePacket =
    let dataLength = 24
    let empty = OtherPacket (Array.zeroCreate dataLength)

module Sector =
    let packetCount = 4
