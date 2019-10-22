﻿using System;
using System.Collections;
using System.IO;
using System.Linq;
using uTinyRipper.AssetExporters;
using uTinyRipper.Classes;
using uTinyRipper.Classes.Meshes;

namespace uTinyRipper.Converters.Meshes
{
	public static class VertexDataConverter
	{
		public static VertexData Convert(IExportContainer container, Mesh originMesh)
		{
			VertexData instance = new VertexData();
			if (VertexData.HasCurrentChannels(container.ExportVersion))
			{
				instance.CurrentChannels = GetCurrentChannels(container, ref originMesh.VertexData);
			}
			instance.VertexCount = originMesh.VertexData.VertexCount;
			if (VertexData.HasChannels(container.ExportVersion))
			{
				instance.Channels = GetChannels(container, originMesh);
			}
			if (VertexData.HasStreams(container.ExportVersion))
			{
				instance.Streams = originMesh.VertexData.Streams.ToArray();
			}
			instance.Data = GetData(container, originMesh, ref instance);
			return instance;
		}

		private static uint GetCurrentChannels(IExportContainer container, ref VertexData origin)
		{
			if (VertexData.HasCurrentChannels(container.Version))
			{
				if (container.Version.IsLess(5))
				{
					BitArray curBits = new BitArray(BitConverter.GetBytes(origin.CurrentChannels));
					curBits.Set((int)ShaderChannelV5.Tangent, curBits.Get((int)ShaderChannelV4.Tangent));
					curBits.Set((int)ShaderChannelV4.Tangent, false);
					return curBits.ToUInt32();
				}
				else
				{
					return origin.CurrentChannels;
				}
			}
			else // Version >= 2018
			{
				// TEMP: downgrade
				BitArray curChannels = new BitArray(32);
				for (int i = 0; i < origin.Channels.Length; i++)
				{
					if (origin.Channels[i].IsSet)
					{
						switch ((ShaderChannelV2018)i)
						{
							case ShaderChannelV2018.Vertex:
								curChannels[(int)ShaderChannelV5.Vertex] = true;
								break;
							case ShaderChannelV2018.Normal:
								curChannels[(int)ShaderChannelV5.Normal] = true;
								break;
							case ShaderChannelV2018.Color:
								curChannels[(int)ShaderChannelV5.Color] = true;
								break;
							case ShaderChannelV2018.UV0:
								curChannels[(int)ShaderChannelV5.UV0] = true;
								break;
							case ShaderChannelV2018.UV1:
								curChannels[(int)ShaderChannelV5.UV1] = true;
								break;
							case ShaderChannelV2018.UV2:
								curChannels[(int)ShaderChannelV5.UV2] = true;
								break;
							case ShaderChannelV2018.UV3:
								curChannels[(int)ShaderChannelV5.UV3] = true;
								break;
							case ShaderChannelV2018.Tangent:
								curChannels[(int)ShaderChannelV5.Tangent] = true;
								break;
						}
					}
				}
				return curChannels.ToUInt32();
			}
		}

		private static ChannelInfo[] GetChannels(IExportContainer container, Mesh originMesh)
		{
			ref VertexData origin = ref originMesh.VertexData;
			if (!VertexData.HasChannels(container.Version)) // Version < 4.0.0 - convert streams to channels
			{
				if (container.ExportVersion.IsLess(5))
				{
					ChannelInfo[] channels = new ChannelInfo[6];
					channels[(int)ShaderChannelV4.Vertex] = StreamInfoConverter.GenerateChannelInfo(container, origin.Streams, ShaderChannel.Vertex);
					channels[(int)ShaderChannelV4.Normal] = StreamInfoConverter.GenerateChannelInfo(container, origin.Streams, ShaderChannel.Normal);
					channels[(int)ShaderChannelV4.Color] = StreamInfoConverter.GenerateChannelInfo(container, origin.Streams, ShaderChannel.Color);
					channels[(int)ShaderChannelV4.UV0] = StreamInfoConverter.GenerateChannelInfo(container, origin.Streams, ShaderChannel.UV0);
					channels[(int)ShaderChannelV4.UV1] = StreamInfoConverter.GenerateChannelInfo(container, origin.Streams, ShaderChannel.UV1);
					channels[(int)ShaderChannelV4.Tangent] = StreamInfoConverter.GenerateChannelInfo(container, origin.Streams, ShaderChannel.Tangent);
					return channels;
				}
				else if (VertexData.ToSerializedVersion(container.ExportVersion) < 2)
				{
					ChannelInfo[] channels = new ChannelInfo[8];
					channels[(int)ShaderChannelV5.Vertex] = StreamInfoConverter.GenerateChannelInfo(container, origin.Streams, ShaderChannel.Vertex);
					channels[(int)ShaderChannelV5.Normal] = StreamInfoConverter.GenerateChannelInfo(container, origin.Streams, ShaderChannel.Normal);
					channels[(int)ShaderChannelV5.Color] = StreamInfoConverter.GenerateChannelInfo(container, origin.Streams, ShaderChannel.Color);
					channels[(int)ShaderChannelV5.UV0] = StreamInfoConverter.GenerateChannelInfo(container, origin.Streams, ShaderChannel.UV0);
					channels[(int)ShaderChannelV5.UV1] = StreamInfoConverter.GenerateChannelInfo(container, origin.Streams, ShaderChannel.UV1);
					channels[(int)ShaderChannelV5.Tangent] = StreamInfoConverter.GenerateChannelInfo(container, origin.Streams, ShaderChannel.Tangent);
					return channels;
				}
				else
				{
					ChannelInfo[] channels = new ChannelInfo[14];
					channels[(int)ShaderChannelV2018.Vertex] = StreamInfoConverter.GenerateChannelInfo(container, origin.Streams, ShaderChannel.Vertex);
					channels[(int)ShaderChannelV2018.Normal] = StreamInfoConverter.GenerateChannelInfo(container, origin.Streams, ShaderChannel.Normal);
					channels[(int)ShaderChannelV2018.Tangent] = StreamInfoConverter.GenerateChannelInfo(container, origin.Streams, ShaderChannel.Tangent);
					channels[(int)ShaderChannelV2018.Color] = StreamInfoConverter.GenerateChannelInfo(container, origin.Streams, ShaderChannel.Color);
					channels[(int)ShaderChannelV2018.UV0] = StreamInfoConverter.GenerateChannelInfo(container, origin.Streams, ShaderChannel.UV0);
					channels[(int)ShaderChannelV2018.UV1] = StreamInfoConverter.GenerateChannelInfo(container, origin.Streams, ShaderChannel.UV1);
					ConvertSkinChannels(container, originMesh, channels);
					return channels;
				}
			}
			else if (container.Version.IsLess(5)) // 4.0.0 <= Version < 5.0.0 - convert ShaderChannelV4 to a new channel type
			{
				if (container.ExportVersion.IsLess(5))
				{
					return origin.Channels.ToArray();
				}
				else
				{
					if (VertexData.ToSerializedVersion(container.ExportVersion) < 2)
					{
						ChannelInfo[] channels = new ChannelInfo[8];
						channels[(int)ShaderChannelV5.Vertex] = ChannelInfoConverter.Convert(container, origin.Channels[(int)ShaderChannelV4.Vertex]);
						channels[(int)ShaderChannelV5.Normal] = ChannelInfoConverter.Convert(container, origin.Channels[(int)ShaderChannelV4.Normal]);
						channels[(int)ShaderChannelV5.Color] = ChannelInfoConverter.Convert(container, origin.Channels[(int)ShaderChannelV4.Color]);
						channels[(int)ShaderChannelV5.UV0] = ChannelInfoConverter.Convert(container, origin.Channels[(int)ShaderChannelV4.UV0]);
						channels[(int)ShaderChannelV5.UV1] = ChannelInfoConverter.Convert(container, origin.Channels[(int)ShaderChannelV4.UV1]);
						channels[(int)ShaderChannelV5.Tangent] = ChannelInfoConverter.Convert(container, origin.Channels[(int)ShaderChannelV4.Tangent]);
						return channels;
					}
					else
					{
						ChannelInfo[] channels = new ChannelInfo[14];
						channels[(int)ShaderChannelV2018.Vertex] = ChannelInfoConverter.Convert(container, origin.Channels[(int)ShaderChannelV4.Vertex]);
						channels[(int)ShaderChannelV2018.Normal] = ChannelInfoConverter.Convert(container, origin.Channels[(int)ShaderChannelV4.Normal]);
						channels[(int)ShaderChannelV2018.Tangent] = ChannelInfoConverter.Convert(container, origin.Channels[(int)ShaderChannelV4.Tangent]);
						channels[(int)ShaderChannelV2018.Color] = ChannelInfoConverter.Convert(container, origin.Channels[(int)ShaderChannelV4.Color]);
						channels[(int)ShaderChannelV2018.UV0] = ChannelInfoConverter.Convert(container, origin.Channels[(int)ShaderChannelV4.UV0]);
						channels[(int)ShaderChannelV2018.UV1] = ChannelInfoConverter.Convert(container, origin.Channels[(int)ShaderChannelV4.UV1]);
						ConvertSkinChannels(container, originMesh, channels);
						return channels;
					}
				}
			}
			else if (VertexData.ToSerializedVersion(container.Version) < 2) // 5.0.0 <= Version < 2018.1 - convert ShaderChannelV5 to ShaderChannelV2018
			{
				if (VertexData.ToSerializedVersion(container.ExportVersion) < 2)
				{
					return origin.Channels.ToArray();
				}
				else
				{
					ChannelInfo[] channels = new ChannelInfo[14];
					channels[(int)ShaderChannelV2018.Vertex] = ChannelInfoConverter.Convert(container, origin.Channels[(int)ShaderChannelV5.Vertex]);
					channels[(int)ShaderChannelV2018.Normal] = ChannelInfoConverter.Convert(container, origin.Channels[(int)ShaderChannelV5.Normal]);
					channels[(int)ShaderChannelV2018.Tangent] = ChannelInfoConverter.Convert(container, origin.Channels[(int)ShaderChannelV5.Tangent]);
					channels[(int)ShaderChannelV2018.Color] = ChannelInfoConverter.Convert(container, origin.Channels[(int)ShaderChannelV5.Color]);
					channels[(int)ShaderChannelV2018.UV0] = ChannelInfoConverter.Convert(container, origin.Channels[(int)ShaderChannelV5.UV0]);
					channels[(int)ShaderChannelV2018.UV1] = ChannelInfoConverter.Convert(container, origin.Channels[(int)ShaderChannelV5.UV1]);
					channels[(int)ShaderChannelV2018.UV2] = ChannelInfoConverter.Convert(container, origin.Channels[(int)ShaderChannelV5.UV2]);
					channels[(int)ShaderChannelV2018.UV3] = ChannelInfoConverter.Convert(container, origin.Channels[(int)ShaderChannelV5.UV3]);
					ConvertSkinChannels(container, originMesh, channels);
					return channels;
				}
			}
			else // 2018.1 <= Version
			{
				// TEMP: downgrade
				if (VertexData.ToSerializedVersion(container.ExportVersion) < 2)
				{
					ChannelInfo[] channels = new ChannelInfo[8];
					channels[(int)ShaderChannelV5.Vertex] = ChannelInfoConverter.Convert(container, origin.Channels[(int)ShaderChannelV2018.Vertex]);
					channels[(int)ShaderChannelV5.Normal] = ChannelInfoConverter.Convert(container, origin.Channels[(int)ShaderChannelV2018.Normal]);
					channels[(int)ShaderChannelV5.Color] = ChannelInfoConverter.Convert(container, origin.Channels[(int)ShaderChannelV2018.Color]);
					channels[(int)ShaderChannelV5.UV0] = ChannelInfoConverter.Convert(container, origin.Channels[(int)ShaderChannelV2018.UV0]);
					channels[(int)ShaderChannelV5.UV1] = ChannelInfoConverter.Convert(container, origin.Channels[(int)ShaderChannelV2018.UV1]);
					channels[(int)ShaderChannelV5.Tangent] = ChannelInfoConverter.Convert(container, origin.Channels[(int)ShaderChannelV2018.Tangent]);
					RecalculateChannelOffsets(container, channels);
					return channels;
				}
				else
				{
					return origin.Channels.ToArray();
				}
			}
		}

		private static void ConvertSkinChannels(IExportContainer container, Mesh origin, ChannelInfo[] channels)
		{
			if (origin.Skin.Length > 0)
			{
				byte skinStream = (byte)(channels.Where(t => t.IsSet).Max(t => t.Stream) + 1);
				byte offset = 0;

				VertexFormat weightVFormat = ShaderChannel.SkinWeight.GetVertexFormat(container.ExportVersion);
				byte weightFormat = weightVFormat.ToFormat(container.ExportVersion);
				byte weightDimention = BoneWeights4.Dimention;
				channels[(int)ShaderChannelV2018.SkinWeight] = new ChannelInfo(skinStream, offset, weightFormat, weightDimention);
				offset += (byte)(BoneWeights4.Dimention * weightVFormat.GetSize());

				VertexFormat indexVFormat = ShaderChannel.SkinBoneIndex.GetVertexFormat(container.ExportVersion);
				byte indexFormat = indexVFormat.ToFormat(container.ExportVersion);
				byte indexDimention = BoneWeights4.Dimention;
				channels[(int)ShaderChannelV2018.SkinBoneIndex] = new ChannelInfo(skinStream, offset, indexFormat, indexDimention);
			}
		}

		// TEMP: downgrade
		private static void RecalculateChannelOffsets(IExportContainer container, ChannelInfo[] channels)
		{
			int maxStream = channels.Max(t => t.Stream);
			for (int i = 0; i <= maxStream; i++)
			{
				RecalculateChannelOffsets(container, channels, i);
			}
		}

		private static void RecalculateChannelOffsets(IExportContainer container, ChannelInfo[] channels, int stream)
		{
			byte offset = 0;
			for (int i = 0; i < channels.Length; i++)
			{
				ref ChannelInfo channel = ref channels[i];
				if (channel.IsSet && channel.Stream == stream)
				{
					channel.Offset = offset;
					offset += channel.GetStride(container.ExportVersion);
				}
			}
		}

		private static byte[] GetData(IExportContainer container, Mesh originMesh, ref VertexData instance)
		{
			if (NeedCopyData(container, ref originMesh.VertexData, ref instance))
			{
				return CopyChannelsData(container, originMesh, ref instance);
			}
			else if (NeedAppendSkin(container, ref instance))
			{
				return AppendSkin(originMesh);
			}
			else
			{
				byte[] data = originMesh.GetChannelsData();
				return data == originMesh.VertexData.Data ? data.ToArray() : data;
			}
		}

		private static bool NeedCopyData(IExportContainer container, ref VertexData origin, ref VertexData instance)
		{
			if (container.Platform == Platform.XBox360 && container.ExportPlatform != Platform.XBox360)
			{
				return true;
			}

			// TEMP: downgrade
			if (VertexData.ToSerializedVersion(container.Version) >= 2)
			{
				if (VertexData.ToSerializedVersion(container.ExportVersion) < 2)
				{
					/// we swap v2018 and v5 channels and since Editor up to 2017.x ignore <see cref="ChannelInfo.Offset"/>
					/// value, we have to swap data as well
					return true;
				}
			}

			return false;
		}

		private static bool NeedAppendSkin(IExportContainer container, ref VertexData instance)
		{
			if (VertexData.ToSerializedVersion(container.Version) < 2)
			{
				if (VertexData.ToSerializedVersion(container.ExportVersion) >= 2)
				{
					if (instance.Channels[(int)ShaderChannelV2018.SkinWeight].IsSet)
					{
						return true;
					}
				}
			}
			return false;
		}

		private static byte[] CopyChannelsData(IExportContainer container, Mesh originMesh, ref VertexData instance)
		{
			int maxStream = instance.Channels.Max(t => t.Stream);
			int lastSize = instance.GetStreamSize(container.ExportVersion, maxStream);
			int lastOffset = instance.GetStreamOffset(container.ExportVersion, maxStream);
			byte[] buffer = new byte[lastOffset + lastSize];
			using (MemoryStream dstStream = new MemoryStream(buffer))
			{
				EndianType oendian = container.ExportPlatform == Platform.XBox360 ? EndianType.BigEndian : EndianType.LittleEndian;
				using (EndianWriter dst = new EndianWriter(dstStream, oendian))
				{
					using (MemoryStream srcStream = new MemoryStream(originMesh.GetChannelsData()))
					{
						EndianType iendian = container.Platform == Platform.XBox360 ? EndianType.BigEndian : EndianType.LittleEndian;
						using (EndianReader src = new EndianReader(srcStream, iendian))
						{
							CopyChannelsData(container, ref originMesh.VertexData, ref instance, src, dst);
							if (NeedAppendSkin(container, ref instance))
							{
								dstStream.Position = lastOffset;
								AppendSkin(originMesh.Skin, dst);
							}
						}
					}
				}
			}
			return buffer;
		}

		private static void CopyChannelsData(IExportContainer container, ref VertexData origin, ref VertexData instance, BinaryReader src, BinaryWriter dst)
		{
			for (ShaderChannel c = 0; c <= ShaderChannel.SkinBoneIndex; c++)
			{
				if (!c.HasChannel(container.Version))
				{
					continue;
				}
				ChannelInfo ochannel = origin.Channels[c.ToChannel(container.Version)];
				if (!ochannel.IsSet)
				{
					continue;
				}

				if (!c.HasChannel(container.ExportVersion))
				{
					continue;
				}
				ChannelInfo ichannel = instance.Channels[c.ToChannel(container.ExportVersion)];
				if (!ichannel.IsSet)
				{
					continue;
				}

				int vertexCount = origin.VertexCount;
				int ostride = origin.GetStreamStride(container.Version, ochannel.Stream);
				int istride = instance.GetStreamStride(container.ExportVersion, ichannel.Stream);
				int oextraStride = ostride - ochannel.GetStride(container.Version);
				int iextraStride = istride - ichannel.GetStride(container.ExportVersion);
				src.BaseStream.Position = origin.GetStreamOffset(container.Version, ochannel.Stream) + ochannel.Offset;
				dst.BaseStream.Position = instance.GetStreamOffset(container.ExportVersion, ichannel.Stream) + ichannel.Offset;
				VertexFormat format = ochannel.GetVertexFormat(container.Version);
				switch (format)
				{
					case VertexFormat.Float:
					case VertexFormat.Color:
					case VertexFormat.Int:
						for (int i = 0; i < vertexCount; i++)
						{
							for (int j = 0; j < ochannel.Dimension; j++)
							{
								dst.Write(src.ReadUInt32());
							}
							src.BaseStream.Position += oextraStride;
							dst.BaseStream.Position += iextraStride;
						}
						break;
					case VertexFormat.Float16:
						for (int i = 0; i < vertexCount; i++)
						{
							for (int j = 0; j < ochannel.Dimension; j++)
							{
								dst.Write(src.ReadUInt16());
							}
							src.BaseStream.Position += oextraStride;
							dst.BaseStream.Position += iextraStride;
						}
						break;
					case VertexFormat.Byte:
						for (int i = 0; i < vertexCount; i++)
						{
							for (int j = 0; j < ochannel.Dimension; j++)
							{
								dst.Write(src.ReadByte());
							}
							src.BaseStream.Position += oextraStride;
							dst.BaseStream.Position += iextraStride;
						}
						break;

					default:
						throw new NotSupportedException(ochannel.Format.ToString());
				}
			}
		}

		private static byte[] AppendSkin(Mesh originMesh)
		{
			ref VertexData origin = ref originMesh.VertexData;
			byte[] odata = originMesh.GetChannelsData();
			int dataSize = odata.Length + GetSkinLength(originMesh.Skin);
			byte[] idata = new byte[dataSize];
			Buffer.BlockCopy(odata, 0, idata, 0, odata.Length);
			using (MemoryStream stream = new MemoryStream(idata, odata.Length, idata.Length - odata.Length))
			{
				using (BinaryWriter writer = new BinaryWriter(stream))
				{
					AppendSkin(originMesh.Skin, writer);
				}
			}
			return idata;
		}

		private static void AppendSkin(BoneWeights4[] skin, BinaryWriter writer)
		{
			for (int i = 0; i < skin.Length; i++)
			{
				ref BoneWeights4 weight = ref skin[i];
				writer.Write(weight.Weight0);
				writer.Write(weight.Weight1);
				writer.Write(weight.Weight2);
				writer.Write(weight.Weight3);
				writer.Write(weight.BoneIndex0);
				writer.Write(weight.BoneIndex1);
				writer.Write(weight.BoneIndex2);
				writer.Write(weight.BoneIndex3);
			}
		}

		private static int GetSkinLength(BoneWeights4[] skin)
		{
			int weightSize = BoneWeights4.Dimention * sizeof(float);
			int indexSize = BoneWeights4.Dimention * sizeof(int);
			return (weightSize + indexSize) * skin.Length;
		}
	}
}