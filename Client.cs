using CircularBuffer;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using NWaves;
using NWaves.Filters.Base;
using NWaves.Effects;
using NWaves.Signals.Builders;
using NWaves.FeatureExtractors;
using NWaves.Transforms;
using NWaves.Utils;
using NWaves.Features;

public class Client : IDisposable
{
    //-//Static Elements

    public static int MaxClients = 41;//1 prof+40 students

    //a lot of this is audio stuff...
    public static void Init(int MaxClients, int SampleRate, byte slicesPerSecond)
    {
        Client.SampleRate = SampleRate;
        Client.ClientsList = new List<Client>();
        Client.agregatedPose = new PoseMessage_Server();
        Client.ChunksPerSecond = slicesPerSecond;

        SetAudioSliceOffsets(SampleRate, slicesPerSecond);
    }

    public static void SetAudioSliceOffsets(int audioBufferLength, byte slicesPerSecond)
    {
        AudioChunkOffsets = new int[slicesPerSecond];

        int smallestSlice = SampleRate / slicesPerSecond;
        //I want to make N offsets 0 - end
        for (int i = 0; i < slicesPerSecond; i++)
        {
            AudioChunkOffsets[i] = i * smallestSlice;
            Console.WriteLine($"slice_{i}:{AudioChunkOffsets[i]}");
        }
    }

    //public const int CHUNK_SIZE = 4096;
    //public const int CHUNK_SIZE = 2048;
    //public const int CHUNK_SIZE = 1024;
    public const int CHUNK_SIZE = 512;
    //public const int CHUNK_SIZE = 256;

    //if sample rate is default 8192
    public static int[] AudioChunkOffsets = { 0, 2048, 4096, 6144 }; 

    public static byte ChunksPerSecond;

    //I need to convert these to float, manupulate, then convert back - or I can just do it on the client
    //public CircularBuffer<byte[]> chunksIn;

    List<byte[]> chunksIn;

    int chunksIn_index_read;
    int chunksIn_index_write;

    public CircularBuffer<float> workingData;

    List<byte[]> chunksOut;

    int chunksOut_index_read;
    int chunksOut_index_write;

    public static int SampleRate;

    public static Dictionary<int, bool> clientIDs = new Dictionary<int, bool>(MaxClients);
    public static List<Client> ClientsList;

    public static PoseMessage_Server agregatedPose;

    public static bool AssignID(out int clientId)
    {
        clientId = int.MaxValue;

        for (int i = 0; i < 40; i++)
        {
            if (clientIDs.ContainsKey(i))
            {
                if (clientIDs[i] == false)
                {
                    clientId = i;
                    clientIDs[i] = true;
                    return true;
                }
            }
            else
            {
                clientIDs.Add(i, true);
                clientId = i;
                return true;
            }
        }

        return false;

    }

    //-//-//Member elements

    int ClientID;
    NetworkStream networkStream;

    Dictionary<int, bool> muteList;

    int NextSliceToWrite = 0;
    public bool HasMessages => networkStream.DataAvailable;
    public bool NeedsAudio;
    public bool NeedsPoseData;
    public bool NeedsConfig;
    public DateTime MostRecentMessage;

    //the entire buffer of Client Audio Data
    public byte[] memberAudioData;
    public float[] audioDataIn;
    public float[] audioDataOut;
    public float[] audioData_Float;

    PoseDescription MostRecentPoseDescription;

    //-//Handle Messages
    float[] myfloats;

    public void HandleMessage_Config()
    {
        int qtyBytesRead = 0;
        byte[] bytes = new byte[4];

        qtyBytesRead = networkStream.Read(bytes, 0, ConfigMessage.DATA_SIZE);

        myfloats = new float[1];

        Buffer.BlockCopy(bytes, 0, myfloats, 0, sizeof(float));

        Console.WriteLine($"received float: {myfloats[0]}");
        
        NeedsConfig = true;
    }

    public void HandleMessage_Pose()
    {
        NeedsPoseData = true;
        //lol, hacky, not sure what's going to happen
        _ = networkStream.Read(ByteTypeConverter.SbytesTobytes(MostRecentPoseDescription.data), 0, PoseDescription.DATA_SIZE);
    }

    int IncrementBufferIndex(ref int i)
    {
        int output = i;
        i = (i + 1) % BUFFER_COUNT;
        return output;
    }

    //rightnow we're writing to the out stream, but we should be writing to the float stream which will be processed into the out stream

    public void ReadAudioMessageToBuffer()
    {
        Console.WriteLine($"Starting audio message read");

        int bytesRead = 0;

        int expectedSize = CHUNK_SIZE * sizeof(float);

        var writeBuffer = chunksIn[IncrementBufferIndex(ref chunksIn_index_write)];

        //this will break everything if we don't wait correctly - fine for now, but multiple clients will need proper async

        //if this blocks, then we get stuck
        while (bytesRead < expectedSize)
        {
            bytesRead += networkStream.ReadAsync(writeBuffer, bytesRead, expectedSize - bytesRead).Result;

            Console.WriteLine($"read {bytesRead} bytes");
        }

        var readFrom = chunksIn[IncrementBufferIndex(ref chunksIn_index_read)];

        //var writeTo = chunksOut[IncrementBufferIndex(ref chunksOut_index_write)];

        //write first front of in queue to back of out queue
        Buffer.BlockCopy(readFrom, 0, audioDataIn, 0, expectedSize);
    }

    void processAudioEffects()
    {
        var p = Pitch.FromSpectralPeaks(audioDataIn, 48000, 10, 100);
        delreadDriver.SetParameter("frequency", (p * 0.37) + 0.5/*Hz*/);

        for (int i = 0; i < CHUNK_SIZE; i++)
        {
            delayLine.Write(audioDataIn[i]);
            audioDataOut[i] = pitchDown.Process(delayLine.Read(48 * (24.72 + (delreadDriver.NextSample() * 22.69))));
        }

        for (int i = 0; i < CHUNK_SIZE; i++)
        {
            lop.Process(audioDataOut[i]);
        }

        for (int i = 0; i < CHUNK_SIZE; i++)
        {
            hip.Process(audioDataOut[i]);
        }

        var writeTo = chunksOut[IncrementBufferIndex(ref chunksOut_index_write)];

        Buffer.BlockCopy(audioDataOut, 0, writeTo, 0, CHUNK_SIZE * sizeof(float));
    }

    //we should start the task to send audio back right when we get this?
    public void HandleMessage_Audio()
    {
        //if we received a new message then they need a new message
        //Task audioMessageWriter = new Task(SendMessage_Audio);
        //audioMessageWriter.Start();
        //audioMessageWriter.Wait();

        SendMessage_Audio();

        //Task audioMessageReader = new Task(ReadAudioMessageToBuffer);
        //audioMessageReader.Start();
        //audioMessageReader.Wait();

        ReadAudioMessageToBuffer();

        //this should likely be split up or whatever
        processAudioEffects();

        Console.WriteLine("\n");
    }

    public void HandleMessage_Error(MessageHeader header)
    {
        //error state recovery
        var dump = new byte[CHUNK_SIZE];
        int errorBytes = 0;
        int totalBytes = 0;
        while (networkStream.DataAvailable)
        {
            errorBytes = networkStream.Read(dump, 0, CHUNK_SIZE);

            totalBytes += errorBytes;

            Console.WriteLine(Encoding.ASCII.GetString(dump));
        }

        Console.WriteLine($"\n\t totalErrorBytes: {totalBytes}\n");

    }

    //we have to make some new message types:
    //Request available rooms
    //add me to a room
    //add me to any room
    //pose stuff
    //

    public void HandleMessages()
    {
        var headerBuffer = new byte[MessageHeader.HEADER_SIZE];

        while (networkStream.DataAvailable)
        {
            int headerSizeIn = networkStream.Read(headerBuffer, 0, MessageHeader.HEADER_SIZE);

            var header = new MessageHeader(headerBuffer);

            //ring buffer here
            Console.WriteLine($"header: type:{header.messageType}, ClientID:{header.ClientID}, DataByte:{header.dataByte}");

            switch (header.messageType)
            {
                case MessageHeader.MessageType.configMessage: HandleMessage_Config(); break;
                case MessageHeader.MessageType.poseMessage: HandleMessage_Pose(); break;
                case MessageHeader.MessageType.audioMessage: HandleMessage_Audio(); break;
                default: HandleMessage_Error(header); break;
            }
        }

        MostRecentMessage = DateTime.UtcNow;
    }

//-//Audio Utils
    public void WriteAudioDataSlice(int slice, byte[] source)
    {
        Buffer.BlockCopy(source, 0, memberAudioData, AudioChunkOffsets[slice], AudioMessage.AudioSliceSize);
    }

    public void ReadAudioDataSlice(int slice, byte[] destination, int destinationOffset)
    {
        Buffer.BlockCopy(memberAudioData, AudioChunkOffsets[slice], destination, 0 + destinationOffset, AudioMessage.AudioSliceSize);
    }


    public void WriteBytesToStream(byte[] bytesToWrite, NetworkStream networkStream)
    {
        
        //there's probably a better way to do this... 
        try
        {
            networkStream.WriteAsync(bytesToWrite, 0, bytesToWrite.Length).Wait();
            networkStream.FlushAsync().Wait();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            //Dispose();
        }
    }

    public void SendMessage_Config()
    {
        var configMessage = new ConfigMessage();
        configMessage.ClientId = (byte)ClientID;
        configMessage.SampleRate = Client.SampleRate;
        configMessage.SlicesPerSecond = Client.ChunksPerSecond;

        WriteBytesToStream(configMessage.GetBytes(), networkStream);

        NeedsConfig = false;
    }

    public void SendMessage_Pose()
    {
        PoseMessage_Server poseMessageOut = new PoseMessage_Server();

        for (int i = 0; i < MaxClients; i++)
        {
            //lol, what a crazy thing to do...
            poseMessageOut.poseDescriptions.Add(ClientsList[i % ClientsList.Count].MostRecentPoseDescription);
        }

        poseMessageOut.header.dataByte = (byte)MaxClients;//if there's ever more than 256 clients, update this

        

        WriteBytesToStream(poseMessageOut.GetBytes(), networkStream);

        NeedsPoseData = false;
    }

    public void SendMessage_Audio()
    {
        var audioMessageToClient = new AudioMessage();

        audioMessageToClient.data = chunksOut[IncrementBufferIndex(ref chunksOut_index_read)];

        audioMessageToClient.header.messageType = MessageHeader.MessageType.audioMessage;

        audioMessageToClient.header.ClientID = (byte)this.ClientID;

        var b = audioMessageToClient.GetBytes();

        networkStream.WriteAsync(b, 0, b.Length).Wait();
        networkStream.FlushAsync().Wait();
    }

    #region ctor and Dispose

    public const int BUFFER_COUNT = 8;


    PitchShiftVocoderEffect pitchDown;
    PitchShiftVocoderEffect pitchUp;

    FractionalDelayLine delayLine; 
    SignalBuilder delreadDriver;

    RealFft realFft;

    IOnlineFilter lop;
    IOnlineFilter hip;

    //Ctor stuff
    public Client(NetworkStream _networkStream)
    {

        realFft = new RealFft(CHUNK_SIZE);

        lop = new NWaves.Filters.OnePole.LowPassFilter(250.0);

        hip = new NWaves.Filters.OnePole.HighPassFilter(60.0);

        delayLine = new FractionalDelayLine(48000, InterpolationMode.Cubic);

        pitchDown = new PitchShiftVocoderEffect(48000, .8, CHUNK_SIZE, 64);
        pitchUp = new PitchShiftVocoderEffect(48000, 2, CHUNK_SIZE, 64);

        delreadDriver = new SineBuilder()
            .SetParameter("frequency", 15.0/*Hz*/)
            .SampledAt(48000/*Hz*/);


        audioDataIn = new float[CHUNK_SIZE];
        audioDataOut = new float[CHUNK_SIZE];

        audioData_Float = new float[SampleRate];//one second of float audio to kick this bad boy off!

        MostRecentMessage = DateTime.UtcNow + TimeSpan.FromSeconds(5);

        chunksIn = new List<byte[]>(BUFFER_COUNT);

        chunksIn_index_write = 0;
        chunksIn_index_read = 1;

        while(chunksIn.Count < BUFFER_COUNT)
        {
            chunksIn.Add(new byte[CHUNK_SIZE * sizeof(float)]);
        }

        chunksOut = new List<byte[]>(BUFFER_COUNT);
        
        chunksOut_index_write = 1;
        chunksOut_index_read = 0;
        
        while (chunksOut.Count < BUFFER_COUNT)
        {
            chunksOut.Add(new byte[CHUNK_SIZE * sizeof(float)]);
        }


        if (!AssignID(out ClientID))
        {
            throw new Exception("Server Full");
        }

        Console.WriteLine($"client_{ClientID} connected");

        networkStream = _networkStream;

        //member audio data should be a full second of data I think
        memberAudioData = new byte[SampleRate];

        MostRecentPoseDescription = new PoseDescription();
    }
    public void Dispose()
    {
        ClientsList.Remove(this);
        clientIDs[ClientID] = false;
        Console.WriteLine($"client_{ClientID} disconected");
    }
#endregion //ctor and Dispose
}