using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Engine.Screens;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using Module = TaleWorlds.MountAndBlade.Module;
using TaleWorlds.SaveSystem;
using TaleWorlds.MountAndBlade.View.Missions;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.SaveLoad;
using SandBox;
using TaleWorlds.InputSystem;
using System.IO;
using TaleWorlds.Library;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade.Source.Missions;
using SandBox.Source.Missions;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers;
using SandBox.Source.Missions.Handlers;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TaleWorlds.ObjectSystem;

namespace CoopTestMod
{

    //[Serializable]
    //public class AgentSerilizer : CustomSerializer
    //{
    //    public AgentSerilizer(Agent agent) : base(agent)
    //    {

    //    }
    //    Dictionary<FieldInfo, ICustomSerializer> SNNSO = new Dictionary<FieldInfo, ICustomSerializer>();
    //    public override object Deserialize()
    //    {



    //        foreach (KeyValuePair<FieldInfo, ICustomSerializer> entry in SNNSO)
    //        {
    //            entry.Key.SetValue(newClan, entry.Value.Deserialize());
    //        }
    //        base.Deserialize(newClan);
    //    }

    //    public override void ResolveReferenceGuids()
    //    {
    //        throw new NotImplementedException();
    //    }
    //}


    public class MySubModule : MBSubModuleBase
    {



        private Settlement _settlement;
        private Agent _otherAgent;
        private Agent _player;

        private Socket sender;
        private Socket listener;
        private Socket handler;
        private bool isServer = false;
        private UIntPtr agentPtr;
        private UIntPtr otherAgentPtr;
        Func<UIntPtr, Vec3> getPosition;
        MyAction setPosition;
        List<MatrixFrame> _initialSpawnFrames;
        IPEndPoint remoteEP;
        MatrixFrame randomElement2;

        float t;




        delegate void MyAction(UIntPtr agentPtr, ref Vec3 position);

        public bool ClientConnect(IPEndPoint remoteEP)
        {
            try
            {
                sender.Connect(remoteEP);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public void StartServer()
        {
            // Get Host IP Address that is used to establish a connection
            // In this case, we get one IP address of localhost that is IP : 127.0.0.1
            // If a host has multiple addresses, you will get a list of addresses
            IPHostEntry host = Dns.GetHostEntry("localhost");
            IPAddress ipAddress = host.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

            try
            {

                // Create a Socket that will use Tcp protocol
                listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                // A Socket must be associated with an endpoint using the Bind method
                listener.Bind(localEndPoint);
                // Specify how many requests a Socket can listen before it gives Server busy response.
                // We will listen 10 requests at a time
                listener.Listen(10);

                Console.WriteLine("Waiting for a connection...");
                handler = listener.Accept();

                // Incoming data from the client.

                byte[] bytes = null;

                while (true)
                {
                    bytes = new byte[1024];
                    int bytesRec = handler.Receive(bytes);

                    float x = BitConverter.ToSingle(bytes, 0);
                    float y = BitConverter.ToSingle(bytes, 4);
                    float z = BitConverter.ToSingle(bytes, 8);
                    Vec3 pos = new Vec3(x, y, z);
                    //InformationManager.DisplayMessage(new InformationMessage("x: " + x + " | y: " + y + " | z: " + z));
                    if (Mission.Current != null && agentPtr != UIntPtr.Zero &&  otherAgentPtr != UIntPtr.Zero)
                    {
                        setPosition(otherAgentPtr, ref pos);
                    }
                    

                }





            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }
        }

        public void StartClient()
        {
            byte[] bytes = new byte[1024];

            try
            {
                // Connect to a Remote server
                // Get Host IP Address that is used to establish a connection
                // In this case, we get one IP address of localhost that is IP : 127.0.0.1
                // If a host has multiple addresses, you will get a list of addresses
                IPHostEntry host = Dns.GetHostEntry("localhost");
                IPAddress ipAddress = host.AddressList[0];
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);

                // Create a TCP/IP  socket.
                sender = new Socket(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);

                // Connect the socket to the remote endpoint. Catch any errors.
                try
                {
                    // Connect to Remote EndPoint
                    while (!ClientConnect(remoteEP))
                    {
                        Console.WriteLine("Unable to connect to server, waiting 5 seconds and trying again");
                        Thread.Sleep(5000);
                    }

                    Console.WriteLine("Socket connected to {0}",
                        sender.RemoteEndPoint.ToString());

                    // Encode the data string into a byte array.
                    byte[] msg = Encoding.ASCII.GetBytes("This is a test");


                    // Receive the response from the remote device.
                    while (true)
                    {
                        bytes = new byte[1024];
                        int bytesRec = sender.Receive(bytes);

                        float x = BitConverter.ToSingle(bytes, 0);
                        float y = BitConverter.ToSingle(bytes, 4);
                        float z = BitConverter.ToSingle(bytes, 8);
                        Vec3 pos = new Vec3(x, y, z);
                        //InformationManager.DisplayMessage(new InformationMessage("x: " + x + " | y: " + y + " | z: " + z));
                        if (Mission.Current != null && agentPtr != UIntPtr.Zero && otherAgentPtr != UIntPtr.Zero)
                        {
                            
                            
                            setPosition(otherAgentPtr, ref pos);
                        }
                    }


                    // Release the socket.
                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();

                }
                catch (ArgumentNullException ane)
                {
                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                }
                catch (SocketException se)
                {
                    Console.WriteLine("SocketException : {0}", se.ToString());
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }






        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();





            Thread thread = null;
            string[] array = Utilities.GetFullCommandLineString().Split(' ');
            foreach (string argument in array)
            {
                if (argument.ToLower() == "/server")
                {
                    isServer = true;
                    // InformationManager.DisplayMessage(new InformationMessage("Running as server!"));
                    thread = new Thread(StartServer);
                }
                else if (argument.ToLower() == "/client")
                {
                    thread = new Thread(StartClient);
                    //InformationManager.DisplayMessage(new InformationMessage("Running as client!"));
                }
            }
            thread.IsBackground = true;
            thread.Start();


        }

        public override void OnGameLoaded(Game game, object initializerObject)
        {
            base.OnGameLoaded(game, initializerObject);
        }


        private Agent SpawnArenaAgent(CharacterObject character, MatrixFrame frame)
        {
            AgentBuildData agentBuildData = new AgentBuildData(character);
            agentBuildData.BodyProperties(character.GetBodyPropertiesMax());
            Mission mission = Mission.Current;
            AgentBuildData agentBuildData2 = agentBuildData.Team((character == CharacterObject.PlayerCharacter) ? Mission.Current.PlayerTeam : Mission.Current.PlayerEnemyTeam).InitialPosition(frame.origin);
            Vec2 vec = frame.rotation.f.AsVec2;
            vec = vec.Normalized();
            Agent agent = mission.SpawnAgent(agentBuildData2.InitialDirection(vec).NoHorses(true).Equipment(character.FirstBattleEquipment).TroopOrigin(new SimpleAgentOrigin(character, -1, null, default(UniqueTroopDescriptor))), false, 0);
            agent.FadeIn();
            if (character == CharacterObject.PlayerCharacter)
            {
                agent.Controller = Agent.ControllerType.Player;

            }
            else
            {
                agent.Controller = Agent.ControllerType.None;
            }
            //if (agent.IsAIControlled)
            //{

            //    agent.SetWatchState(Agent.WatchState.Alarmed);
            //}
            //agent.Health = this._customAgentHealth;
            //agent.BaseHealthLimit = this._customAgentHealth;
            //agent.HealthLimit = this._customAgentHealth;

            return agent;
        }


        protected override void OnApplicationTick(float dt)
        {
            // Press K first to load the Poros arena
            if (Input.IsKeyReleased(InputKey.K))
            {


                // get the settlement first
                this._settlement = Settlement.Find("town_ES3");

                // get its arena
                Location locationWithId = _settlement.LocationComplex.GetLocationWithId("arena");

                CharacterObject characterObject = CharacterObject.PlayerCharacter;
                LocationEncounter locationEncounter = new TownEncounter(_settlement);

                // create an encounter of the town with the player
                EncounterManager.StartSettlementEncounter(MobileParty.MainParty, _settlement);

                //Set our encounter to the created encounter
                PlayerEncounter.LocationEncounter = locationEncounter;

                //return arena scenae name of current town
                int upgradeLevel = _settlement.IsTown ? _settlement.Town.GetWallLevel() : 1;

                //Open a new arena mission with the scene
                MissionState.OpenNew("ArenaDuelMission", SandBoxMissions.CreateSandBoxMissionInitializerRecord(locationWithId.GetSceneName(upgradeLevel), "", false), (Mission mission) => new MissionBehaviour[]
                   {
                            new MissionOptionsComponent(),
                            //new ArenaDuelMissionController(CharacterObject.PlayerCharacter, false, false, null, 1), //this was the default controller that spawned the player and 1 opponent. Not very useful
                            new MissionFacialAnimationHandler(),
                            new MissionDebugHandler(),
                            new MissionAgentPanicHandler(),
                            new AgentBattleAILogic(),
                            new ArenaAgentStateDeciderLogic(),
                            new VisualTrackerMissionBehavior(),
                            new CampaignMissionComponent(),
                            new EquipmentControllerLeaveLogic(),
                            new MissionAgentHandler(locationWithId, null)
                   }, true, true);





            }

            // Press slash next to spawn in the arena
            else if (Input.IsKeyReleased(InputKey.Slash))
            {
                //two teams are created
                Mission.Current.Teams.Add(BattleSideEnum.Defender, Hero.MainHero.MapFaction.Color, Hero.MainHero.MapFaction.Color2, null, true, false, true);
                Mission.Current.Teams.Add(BattleSideEnum.Attacker, Hero.MainHero.MapFaction.Color2, Hero.MainHero.MapFaction.Color, null, true, false, true);

                //players is defender team
                Mission.Current.PlayerTeam = Mission.Current.DefenderTeam;


                //find areas of spawn

                _initialSpawnFrames = (from e in Mission.Current.Scene.FindEntitiesWithTag("sp_arena")
                                       select e.GetGlobalFrame()).ToList();
                for (int i = 0; i < _initialSpawnFrames.Count; i++)
                {
                    MatrixFrame value = _initialSpawnFrames[i];
                    value.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
                    _initialSpawnFrames[i] = value;
                }
                // get a random spawn point
                MatrixFrame randomElement = _initialSpawnFrames.GetRandomElement();
                //remove the point so no overlap
                _initialSpawnFrames.Remove(randomElement);
                //find another spawn point
                randomElement2 = _initialSpawnFrames.GetRandomElement();


                // spawn an instance of the player (controlled by default)
                _player = SpawnArenaAgent(CharacterObject.PlayerCharacter, randomElement);


                //spawn another instance of the player, uncontroller (this should get synced when someone joins)
                _otherAgent = SpawnArenaAgent(CharacterObject.All.LastOrDefault(), randomElement2);


                // Our agent's pointer; set it to 0 first
                agentPtr = UIntPtr.Zero;


                // other agent's pointer
                otherAgentPtr = (UIntPtr)_otherAgent.GetType().GetField("_pointer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_otherAgent);


                //// Find out agent's pointer from our agent instance
                agentPtr = (UIntPtr)_player.GetType().GetField("_pointer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_player);

                //// From MBAPI, get the private interface IMBAgent
                FieldInfo IMBAgentField = typeof(MBAPI).GetField("IMBAgent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                MethodInfo getPositionMethod = IMBAgentField.GetValue(null).GetType().GetMethod("GetPosition");
                MethodInfo setPositionMethod = IMBAgentField.GetValue(null).GetType().GetMethod("SetPosition");

                if (setPositionMethod == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage("Set Method is null!"));
                }
                else
                {
                    ParameterInfo[] par = setPositionMethod.GetParameters();
                    foreach (ParameterInfo param in par)
                    {
                        InformationManager.DisplayMessage(new InformationMessage(param.ParameterType.Name));
                    }

                }

                getPosition = (Func<UIntPtr, Vec3>)Delegate.CreateDelegate
                    (typeof(Func<UIntPtr, Vec3>), IMBAgentField.GetValue(null), getPositionMethod);

                try
                {
                    setPosition = (MyAction)Delegate.CreateDelegate(typeof(MyAction), IMBAgentField.GetValue(null), setPositionMethod);
                }
                catch (Exception ex)
                {
                    File.AppendAllText("werror.txt", ex.Message);
                }



            }

            //else if (Input.IsKeyDown(InputKey.W) || Input.IsKeyDown(InputKey.A) || Input.IsKeyDown(InputKey.S) || Input.IsKeyDown(InputKey.D))
            //{
            //    if (isServer)
            //    {

            //    }
            //}

            else if (Input.IsKeyReleased(InputKey.Numpad0))
            {
                if (isServer)
                {
                    if (handler != null && handler.Connected)
                    {
                        handler.Send(Encoding.ASCII.GetBytes("Hello from Server!"));
                    }

                }
                else
                {
                    if (sender != null && sender.Connected)
                    {
                        sender.Send(Encoding.ASCII.GetBytes("Hello from Client!"));
                    }

                }
            }
            else if (Input.IsKeyReleased(InputKey.Numpad1))
            {

                MemoryStream stream = new MemoryStream();
                using (System.IO.BinaryWriter writer = new System.IO.BinaryWriter(stream))
                {
                    writer.Write(408.5537f);
                    writer.Write(433.4336f);
                    writer.Write(-0.9081879f);
                }
                byte[] bytes = stream.ToArray();
                if (isServer)
                {
                    handler.Send(bytes);
                }

                else
                {
                    sender.Send(bytes);
                }
                //InformationManager.DisplayMessage(new InformationMessage("Getting pointer info: "));
                //UIntPtr agentPtr = UIntPtr.Zero;
                //agentPtr = (UIntPtr)_player.GetType().GetField("_pointer", BindingFlags.Instance  | BindingFlags.NonPublic).GetValue(_player);
                //InformationManager.DisplayMessage(new InformationMessage(agentPtr.ToString()));
                //object agent = null;

                //InformationManager.DisplayMessage(new InformationMessage("Printing data:"));
                //FieldInfo field = typeof(MBAPI).GetField("IMBAgent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                //MethodInfo[] fields = field.GetValue(null).GetType().GetMethods();
                //Vec3 vect = Vec3.Invalid;
                //MethodInfo method = field.GetValue(null).GetType().GetMethod("GetPosition");

                //if (method == null)
                //{
                //    InformationManager.DisplayMessage(new InformationMessage("Not found!"));
                //}
                //else
                //{
                //    InformationManager.DisplayMessage(new InformationMessage("Found!"));
                //}
                //InformationManager.DisplayMessage(new InformationMessage("Size of fields: " + fields.Length));
                //foreach (MethodInfo f in fields)
                //{
                //    InformationManager.DisplayMessage(new InformationMessage(f.Name));
                //    File.AppendAllText("woutput.txt", f.Name + "\n");
                //}



                //MemoryStream stream = new MemoryStream();
                //using (System.IO.BinaryWriter writer = new System.IO.BinaryWriter(stream))
                //{
                //    writer.Write(myByte);
                //    writer.Write(myInt32);
                //    writer.Write("Hello");
                //}
                //byte[] bytes = stream.ToArray();

                //vect = converted(agentPtr);
                //if (vect.IsValid)
                //{
                //    InformationManager.DisplayMessage(new InformationMessage("Vector is valid!"));
                //}
                //else
                //{
                //    InformationManager.DisplayMessage(new InformationMessage("Vector is invalid!"));
                //}
                //InformationManager.DisplayMessage(new InformationMessage("x:" + vect.x + " | y: " + vect.y + " | z: " + vect.z));
            }
            if (Mission.Current != null && agentPtr != UIntPtr.Zero)
            {
                if (t + 0.01 > Time.ApplicationTime)
                {
                    return;
                }
                t = Time.ApplicationTime;
                MemoryStream stream = new MemoryStream();
                Vec3 myPos = getPosition(agentPtr);
                if (myPos.IsValid)
                {
                    using (System.IO.BinaryWriter writer = new System.IO.BinaryWriter(stream))
                    {
                        writer.Write(myPos.x);
                        writer.Write(myPos.y);
                        writer.Write(myPos.z);
                    }
                    byte[] bytes = stream.ToArray();
                    if (isServer && handler != null && handler.Connected)
                    {
                        handler.Send(bytes);
                    }
                    else if (sender != null && sender.Connected)
                    {
                        sender.Send(bytes);
                    }
                    else
                    {
                        InformationManager.DisplayMessage(new InformationMessage("Somehow disconnected"));
                        ClientConnect(remoteEP);
                    }

                }

            }

        }

    }
}