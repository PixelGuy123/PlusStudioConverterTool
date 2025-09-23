using PlusLevelStudio;
using PlusLevelStudio.Editor;
using PlusLevelStudio.Editor.ModeSettings;
using PlusLevelStudio.Lua;
using PlusStudioLevelFormat;
using PlusStudioLevelLoader;

namespace PlusStudioConverterTool.Extensions;

internal static class AltLevelLoaderExtensions
{
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable IL2075 // 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.
    public static void RemoveEditorRoomWithoutUnity(this EditorLevelData data, EditorRoom toRemove)
    {
        int roomId = data.rooms.IndexOf(toRemove) + 1;
        for (int i = 0; i < data.areas.Count; i++)
        {
            if (data.areas[i].roomId == roomId)
                data.areas.RemoveAt(i--);
            if (data.areas[i].roomId > roomId)
                data.areas[i].roomId--;
        }
        foreach (var cell in data.cells)
        {
            if (cell.roomId == roomId)
            {
                cell.roomId = 0;
                cell.walls = (Nybble)0; // Turn into normal cell
            }
            if (cell.roomId > roomId)
                cell.roomId--;
        }
        data.rooms.RemoveAt(roomId - 1);
    }
    public static bool ValidatePosition(this EditorLevelData data, UnityEngine.Vector3 position) =>
        data.ValidatePosition(new IntVector2((int)Math.Round((position.x - 5f) / 10f), (int)Math.Round((position.z - 5f) / 10f)));

    public static bool ValidatePosition(this EditorLevelData data, IntVector2 position) =>
        data.RoomFromPos(position, forEditor: true) != null;

    public static EditorFileContainer ReadMindfulSafe(this BinaryReader reader)
    {
        EditorFileContainer editorFileContainer = new();
        long position = reader.BaseStream.Position;
        if (reader.ReadByte() < 2)
        {
            reader.BaseStream.Position = position;
            editorFileContainer.data = ReadWithoutUnity(reader);
            return editorFileContainer;
        }

        reader.BaseStream.Position = position;
        editorFileContainer.meta = EditorFileMeta.Read(reader);
        editorFileContainer.data = ReadWithoutUnity(reader);
        return editorFileContainer;

        static EditorLevelData ReadWithoutUnity(BinaryReader reader) // It was the only way....
        {
            byte b = reader.ReadByte();
            if (b > 9)
            {
                throw new Exception("Attempted to read file with newer version number than the one supported! (Got: " + b + " expected: " + (byte)9 + " or below)");
            }

            StringCompressor stringCompressor = StringCompressor.ReadStringDatabase(reader);
            string text = "WIP";
            if (b == 2)
            {
                text = reader.ReadString();
            }

            EditorLevelData editorLevelData = new(new IntVector2(reader.ReadByte(), reader.ReadByte()))
            {
                elevatorTitle = text
            };
            editorLevelData.lightGroups.Clear();
            editorLevelData.rooms.Clear();
            int num = reader.ReadInt32();
            for (int i = 0; i < num; i++)
            {
                reader.ReadString();
                editorLevelData.areas.Add(new RectCellArea(default, default, 0).ReadInto(reader));
            }

            int num2 = reader.ReadInt32();
            for (int j = 0; j < num2; j++)
            {
                EditorRoom editorRoom = new(stringCompressor.ReadStoredString(reader), new TextureContainer(stringCompressor.ReadStoredString(reader), stringCompressor.ReadStoredString(reader), stringCompressor.ReadStoredString(reader)));
                string text2 = stringCompressor.ReadStoredString(reader);
                if (text2 != "null")
                {
                    ActivityLocation activityLocation = new()
                    {
                        type = text2,
                        position = reader.ReadUnityVector3().ToUnity(),
                        direction = (Direction)reader.ReadByte()
                    };
                    activityLocation.Setup(editorRoom);
                }

                editorLevelData.rooms.Add(editorRoom);
            }

            ushort num3 = reader.ReadUInt16();
            for (int k = 0; k < num3; k++)
            {
                editorLevelData.lightGroups.Add(new LightGroup
                {
                    color = reader.ReadUnityColor().ToStandard(),
                    strength = reader.ReadByte()
                });
            }

            int num4 = reader.ReadInt32();
            for (int l = 0; l < num4; l++)
            {
                editorLevelData.lights.Add(new LightPlacement
                {
                    type = stringCompressor.ReadStoredString(reader),
                    position = PlusStudioLevelLoader.Extensions.ToInt(reader.ReadByteVector2()),
                    lightGroup = reader.ReadUInt16()
                });
            }

            int num5 = reader.ReadInt32();
            for (int m = 0; m < num5; m++)
            {
                editorLevelData.doors.Add(new DoorLocation
                {
                    type = stringCompressor.ReadStoredString(reader),
                    position = PlusStudioLevelLoader.Extensions.ToInt(reader.ReadByteVector2()),
                    direction = (Direction)reader.ReadByte()
                });
            }

            int num6 = reader.ReadInt32();
            for (int n = 0; n < num6; n++)
            {
                editorLevelData.windows.Add(new WindowLocation
                {
                    type = stringCompressor.ReadStoredString(reader),
                    position = PlusStudioLevelLoader.Extensions.ToInt(reader.ReadByteVector2()),
                    direction = (Direction)reader.ReadByte()
                });
            }

            int num7 = reader.ReadInt32();
            for (int num8 = 0; num8 < num7; num8++)
            {
                editorLevelData.exits.Add(new ExitLocation
                {
                    type = stringCompressor.ReadStoredString(reader),
                    position = PlusStudioLevelLoader.Extensions.ToInt(reader.ReadByteVector2()),
                    direction = (Direction)reader.ReadByte(),
                    isSpawn = reader.ReadBoolean()
                });
            }

            int num9 = reader.ReadInt32();
            for (int num10 = 0; num10 < num9; num10++)
            {
                editorLevelData.items.Add(new ItemPlacement
                {
                    item = stringCompressor.ReadStoredString(reader),
                    position = reader.ReadUnityVector2().ToUnity()
                });
            }

            if (b >= 8)
            {
                int num11 = reader.ReadInt32();
                for (int num12 = 0; num12 < num11; num12++)
                {
                    editorLevelData.itemSpawns.Add(new ItemSpawnPlacement
                    {
                        weight = reader.ReadInt32(),
                        position = reader.ReadUnityVector2().ToUnity()
                    });
                }
            }

            int num13 = reader.ReadInt32();
            for (int num14 = 0; num14 < num13; num14++)
            {
                editorLevelData.objects.Add(new BasicObjectLocation
                {
                    prefab = stringCompressor.ReadStoredString(reader),
                    position = reader.ReadUnityVector3().ToUnity(),
                    rotation = reader.ReadUnityQuaternion().ToUnity()
                });
            }

            int num15 = reader.ReadInt32();
            for (int num16 = 0; num16 < num15; num16++)
            {
                string type = stringCompressor.ReadStoredString(reader);
                StructureLocation structureLocation = (StructureLocation)structureTypes[type].GetConstructor([]).Invoke([]);
                structureLocation.type = type;
                structureLocation.ReadInto(editorLevelData, reader, stringCompressor);
                editorLevelData.structures.Add(structureLocation);
            }

            int num17 = reader.ReadInt32();
            for (int num18 = 0; num18 < num17; num18++)
            {
                editorLevelData.npcs.Add(new NPCPlacement
                {
                    npc = stringCompressor.ReadStoredString(reader),
                    position = PlusStudioLevelLoader.Extensions.ToInt(reader.ReadByteVector2())
                });
            }

            int num19 = reader.ReadInt32();
            for (int num20 = 0; num20 < num19; num20++)
            {
                editorLevelData.posters.Add(new PosterPlacement
                {
                    type = stringCompressor.ReadStoredString(reader),
                    position = PlusStudioLevelLoader.Extensions.ToInt(reader.ReadByteVector2()),
                    direction = (Direction)reader.ReadByte()
                });
            }

            if (b < 7)
            {
                editorLevelData.meta = new PlayableLevelMeta
                {
                    name = editorLevelData.elevatorTitle,
                    modeSettings = gameModeAliases["standard"].CreateSettings(),
                    gameMode = "standard",
                    contentPackage = new EditorCustomContentPackage(filePaths: true)
                };
            }

            if (b == 0)
            {
                editorLevelData.spawnPoint = reader.ReadUnityVector3().ToUnity();
                editorLevelData.spawnDirection = (Direction)reader.ReadByte();
                return editorLevelData;
            }

            int num21 = reader.ReadInt32();
            for (int num22 = 0; num22 < num21; num22++)
            {
                editorLevelData.walls.Add(new WallLocation
                {
                    wallState = true,
                    position = PlusStudioLevelLoader.Extensions.ToInt(reader.ReadByteVector2()),
                    direction = (Direction)reader.ReadByte()
                });
            }

            int num23 = reader.ReadInt32();
            for (int num24 = 0; num24 < num23; num24++)
            {
                editorLevelData.walls.Add(new WallLocation
                {
                    wallState = false,
                    position = PlusStudioLevelLoader.Extensions.ToInt(reader.ReadByteVector2()),
                    direction = (Direction)reader.ReadByte()
                });
            }

            if (b >= 9)
            {
                int num25 = reader.ReadInt32();
                for (int num26 = 0; num26 < num25; num26++)
                {
                    string type2 = stringCompressor.ReadStoredString(reader);

                    MarkerLocation markerLocation = (MarkerLocation)markerTypes[type2].GetConstructor([]).Invoke([]);
                    markerLocation.type = type2;

                    markerLocation.ReadInto(editorLevelData, reader, stringCompressor);
                    editorLevelData.markers.Add(markerLocation);
                }
            }

            editorLevelData.spawnPoint = reader.ReadUnityVector3().ToUnity();
            editorLevelData.spawnDirection = (Direction)reader.ReadByte();
            if (b < 3)
            {
                return editorLevelData;
            }

            editorLevelData.elevatorTitle = reader.ReadString();
            if (b >= 4)
            {
                editorLevelData.timeLimit = reader.ReadSingle();
            }

            editorLevelData.initialRandomEventGap = reader.ReadSingle();
            editorLevelData.minRandomEventGap = reader.ReadSingle();
            editorLevelData.maxRandomEventGap = reader.ReadSingle();
            int num27 = reader.ReadInt32();
            for (int num28 = 0; num28 < num27; num28++)
            {
                editorLevelData.randomEvents.Add(reader.ReadString());
            }

            if (b <= 4)
            {
                return editorLevelData;
            }

            editorLevelData.skybox = reader.ReadString();
            if (b <= 5)
            {
                return editorLevelData;
            }

            editorLevelData.minLightColor = reader.ReadUnityColor().ToStandard();
            editorLevelData.lightMode = (LightMode)reader.ReadByte();
            if (b <= 6)
            {
                return editorLevelData;
            }

            editorLevelData.meta = reader.ReadPlayableLevelMetaWithoutEditor(true);
            return editorLevelData;
        }
    }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore IL2075 // 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.
    public static PlayableEditorLevel ReadPlayableLevelWithoutThumbnail(this BinaryReader reader, out byte[]? thumbnailData)
    {
        PlayableEditorLevel playableEditorLevel = new();
        thumbnailData = null;
        int num;
        if (reader.ReadByte() >= 1)
        {
            num = reader.ReadInt32();
            if (num > 0)
                thumbnailData = reader.ReadBytes(num); // Discard whatever is done here
        }

        playableEditorLevel.meta = reader.ReadPlayableLevelMetaWithoutEditor(false);
        playableEditorLevel.data = BaldiLevel.Read(reader);
        return playableEditorLevel;
    }

    public static PlayableLevelMeta ReadPlayableLevelMetaWithoutEditor(this BinaryReader reader, bool useOldFilePaths)
    {
        // ** Manually load PlayableLevelMeta to prevent using StudioPlugin
        PlayableLevelMeta playableLevelMeta = new();
        byte byNum = reader.ReadByte();
        playableLevelMeta.name = reader.ReadString();
        if (byNum >= 1)
        {
            playableLevelMeta.author = reader.ReadString();
        }

        playableLevelMeta.gameMode = reader.ReadString();
        bool readIntoBoolean = reader.ReadBoolean();
        if (gameModeAliases.ContainsKey(playableLevelMeta.gameMode))
        {
            if (!readIntoBoolean)
            {
                playableLevelMeta.modeSettings = gameModeAliases[playableLevelMeta.gameMode].CreateSettings();
            }
            else
            {
                playableLevelMeta.modeSettings = gameModeAliases[playableLevelMeta.gameMode].CreateSettings();
                playableLevelMeta.modeSettings.ReadInto(reader);
            }
        }

        if (byNum < 2)
        {
            playableLevelMeta.contentPackage = new EditorCustomContentPackage(useOldFilePaths);
        }
        else
        {
            playableLevelMeta.contentPackage = EditorCustomContentPackage.Read(reader);
        }
        return playableLevelMeta;
    }

    internal static void InitializeSettings()
    {
        gameModeAliases.Add("standard", new MainGameMode());
        gameModeAliases.Add("stealthy", new StealthyGameMode());
        gameModeAliases.Add("custom", new CustomChallengeGameMode());
    }

    readonly static Dictionary<string, EditorGameMode> gameModeAliases = [];
    readonly static Dictionary<string, Type> structureTypes = new()
    {
        { "conveyorbelt", typeof(ConveyorBeltStructureLocation) },
        { "halldoor", typeof(HallDoorStructureLocation) },
        { "swingdoor", typeof(HallDoorStructureLocation) },
        { "facultyonlydoor", typeof(HallDoorStructureLocation) },
        { "regionlockdowndoors", typeof(HallDoorStructureLocation) },
        { "halldoor_button", typeof(HallDoorStructureLocationWithButtons) },
        { "lockdowndoor", typeof(LockdownDoorStructureLocation) },
        { "lockdowndoor_button", typeof(LockdownDoorWithButtonsStructureLocation) },
        { "powerlever", typeof(PowerLeverStructureLocation) },
        { "shapelock", typeof(ShapeLockStructureLocation) },
        { "steamvalves", typeof(SteamValveStructureLocation) },
        { "vent", typeof(VentStructureLocation) },
        { "teleporters", typeof(TeleporterStructureLocation) },
        { "region", typeof(RegionStructureLocation) },
        { "factorybox", typeof(FactoryBoxStructureLocation) },
    };
    readonly static Dictionary<string, Type> markerTypes = new()
    {
        { "matchballoon", typeof(MatchBalloonMarker) }
    };

}