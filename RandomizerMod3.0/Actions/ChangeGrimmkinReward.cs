using System.Linq;
using System.Reflection;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using RandomizerMod.FsmStateActions;
using SeanprCore;
using UnityEngine;
using static RandomizerMod.GiveItemActions;

namespace RandomizerMod.Actions
{
    public class ChangeGrimmkinReward : RandomizerAction
    {
        private readonly string _fsmName;
        private readonly string _objectName;
        private readonly string _sceneName;
        private readonly string _nameKey;
        private readonly string _spriteName;
        private readonly GiveAction _action;
        private readonly string _item;
        private readonly string _location;

        public ChangeGrimmkinReward(string sceneName, string objectName, string fsmName, string nameKey, string spriteName, GiveAction action, string item, string location)
        {
            _sceneName = sceneName;
            _objectName = objectName;
            _fsmName = fsmName;
            _nameKey = nameKey;
            _spriteName = spriteName;
            // GiveItem doesn't support spawning geo, and also there's no shiny to spawn it from anyway.
            if (action == GiveAction.SpawnGeo)
            {
                action = GiveAction.AddGeo;
            }
            _action = action;
            _item = item;
            _location = location;
        }

        public override ActionType Type => ActionType.PlayMakerFSM;

        public override void Process(string scene, Object changeObj)
        {
            if (!(scene == _sceneName && changeObj is PlayMakerFSM fsm))
            {
                return;
            }
            var grimmkinLevel = 1;
            switch (scene)
            {
                case "Tutorial_01":
                case "RestingGrounds_06":
                case "Deepnest_East_03":
                    grimmkinLevel = 2;
                    break;
                case "Fungus2_30":
                case "Abyss_02":
                case "Hive_03":
                    grimmkinLevel = 3;
                    break;
            }
            if (fsm.FsmName == "Control")
            {
                // Make the grimmkin think we have the appropriate
                // Grimmchild level, so that it behaves the same
                // regardless of how much Grimmchild has been
                // upgraded.
                switch (fsm.gameObject.name)
                {
                    case "Flamebearer Small(Clone)":
                    case "Flamebearer Med(Clone)":
                    case "Flamebearer Large(Clone)":
                        FixGrimmkinFSM(fsm, grimmkinLevel);
                        break;
                }
                return;
            }
            if (!(fsm.FsmName == _fsmName && fsm.gameObject.name == _objectName))
            {
                return;
            }

            FsmState get = fsm.GetState("Get");
            get.RemoveActionsOfType<IncrementPlayerDataInt>();
            get.RemoveActionsOfType<SendMessage>();
            get.AddAction(new RandomizerExecuteLambda(() => GiveItem(_action, _item, _location)));

            // Make sure the correct icon and text appear
            get.GetActionsOfType<GetLanguageString>().First().convName = _nameKey;
            get.GetActionsOfType<SetSpriteRendererSprite>().First().sprite = RandomizerMod.GetSprite(_spriteName);

            if (_objectName == "Brumm Torch NPC")
            {
                var checkActive = fsm.GetState("Check Active");
                checkActive.Actions = new FsmStateAction[1];
                checkActive.Actions[0] = new RandomizerExecuteLambda(() => fsm.SendEvent(IsBrummActive() ? "FINISHED" : "INACTIVE"));
                var convo1 = fsm.GetState("Convo 1");
                convo1.RemoveActionsOfType<IntCompare>();
            }
            else
            {
                var init = fsm.GetState("State");
                init.RemoveActionsOfType<IntCompare>();
                init.RemoveActionsOfType<IntSwitch>();
                init.AddAction(new RandomizerExecuteLambda(() => fsm.SendEvent(Ref.PD.GetInt("grimmChildLevel") >= grimmkinLevel ? "LEVEL " + grimmkinLevel : "KILLED")));
            }
        }

        private static bool IsBrummActive()
        {
            var grimmchildLevel = Ref.PD.GetInt("grimmChildLevel");
            return Ref.PD.GetBool("equippedCharm_40") && !Ref.PD.GetBool("gotBrummsFlame") && grimmchildLevel >= 3 && grimmchildLevel < 5;
        }

        private static void FixGrimmkinFSM(PlayMakerFSM fsm, int level)
        {
            RandomizerMod.Instance.Log("fixing grimmkin FSM");
            var init = fsm.GetState("Init");
            var levelVar = init.GetActionsOfType<GetPlayerDataInt>().First().storeValue;
            init.RemoveActionsOfType<GetPlayerDataInt>();
            var fixLevel = new SetIntValue();
            fixLevel.intVariable = levelVar;
            fixLevel.intValue = level;
            fixLevel.everyFrame = false;
            init.AddAction(fixLevel);
        }
    }
}