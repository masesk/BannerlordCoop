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

namespace CoopTestMod
{
    public class MySubModule : MBSubModuleBase
    {



        private Settlement _settlement;


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
            if (agent.IsAIControlled)
            {
                agent.SetWatchState(Agent.WatchState.Alarmed);
            }
            //agent.Health = this._customAgentHealth;
            //agent.BaseHealthLimit = this._customAgentHealth;
            //agent.HealthLimit = this._customAgentHealth;
            return agent;
        }

        protected override void OnApplicationTick(float dt)
        {
            // Press K first to load the Poros arena
            if (Input.IsKeyDown(InputKey.K))
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
            else if (Input.IsKeyDown(InputKey.Slash))
            {
               
                //two teams are created
                Mission.Current.Teams.Add(BattleSideEnum.Defender, Hero.MainHero.MapFaction.Color, Hero.MainHero.MapFaction.Color2, null, true, false, true);
                Mission.Current.Teams.Add(BattleSideEnum.Attacker, Hero.MainHero.MapFaction.Color2, Hero.MainHero.MapFaction.Color, null, true, false, true);

                //players is defender team
                Mission.Current.PlayerTeam = Mission.Current.DefenderTeam;
                List<MatrixFrame> _initialSpawnFrames;

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
                MatrixFrame randomElement2 = _initialSpawnFrames.GetRandomElement();

                // spawn an instance of the player (controlled by default)
                SpawnArenaAgent(CharacterObject.PlayerCharacter, randomElement);

                //spawn another instance of the player, uncontroller (this should get synced when someone joins)
                SpawnArenaAgent(CharacterObject.PlayerCharacter, randomElement2);
            }
        }

    }
}