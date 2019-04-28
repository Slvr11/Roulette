using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InfinityScript;

namespace Roulette
{
    public class roul : BaseScript
    {
        string mapname;
        Vector3 baseSpawn = new Vector3();
        Vector3[] spawnModifiers = new Vector3[16]{ new Vector3(200, 0, 0),
            new Vector3(-200, 0, 0),
            new Vector3(0, 200, 0),
            new Vector3(0, -200, 0),
            new Vector3(100, 100, 0),
            new Vector3(-100, -100, 0),
            new Vector3(-100, 100, 0),
            new Vector3(100, -100, 0),
            new Vector3(150, 50, 0),
            new Vector3(-150, -50, 0),
            new Vector3(150, -50, 0),
            new Vector3(-150, 50, 0),
            new Vector3(50, -150, 0),
            new Vector3(-50, 150, 0),
            new Vector3(-50, -150, 0),
            new Vector3(50, 150, 0)
        };
        Vector3[] spawns = new Vector3[16];
        bool modeStarted = false;

        int currentPlayer = 0;
        int totalShotsTaken = 0;
        bool[] deadPlayers = new bool[16] { true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true };
        HudElem timer;

        public roul()
            : base()
        {
            mapname = Call<string>("getdvar", "mapname");
            int playerLimit = Call<int>("getdvarint", "sv_maxclients");
            if (playerLimit > 16)
            {
                Call("setdvar", "sv_maxclients", 16);
                Call(332, "sv_maxclients", 16);
            }
            Call("setdvar", "ui_allow_classchange", "0");
            Call(332, "ui_allow_classchange", "0");
            Call("setdvar", "ui_allow_teamchange", "0");
            Call(332, "ui_allow_teamchange", "0");
            Call("setdvar", "ui_hud_hardcore", "1");
            Call(332, "ui_hud_hardcore", "1");
            Call("setdvar", "g_hardcore", "1");
            Call(332, "g_hardcore", "1");
            SetupPlayspace();
            SetupHUD();
            //StartRoulette();
            PlayerConnected += new Action<Entity>(player => OnPlayerConnected(player));
        }
        public override void OnPlayerKilled(Entity player, Entity inflictor, Entity attacker, int damage, string mod, string weapon, Vector3 dir, string hitLoc)
        {
            deadPlayers[player.EntRef] = true;
            player.Notify("menuresponse", "team_marinesopfor", "spectator");
            player.SetClientDvar("ui_allow_classchange", "0");
            player.SetClientDvar("ui_allow_teamchange", "0");
            //int score = Call<int>(314, "axis");
            //Call(315, "axis", score + 1);
        }

        private void OnPlayerConnected(Entity player)
        {
            player.AfterDelay(100, (p) =>
            {
                p.Notify("menuresponse", "team_marinesopfor", "axis");
                p.AfterDelay(200, (e) =>
                    e.Notify("menuresponse", "changeclass", "axis_recipe1"));
            });
            player.SetClientDvar("ui_hud_hardcore", "1");
            player.SetClientDvar("g_hardcore", "1");
            player.SetClientDvar("ui_hud_obituaries", "0");
            player.SetClientDvar("cg_drawCrosshair", "0");
            player.SetClientDvar("cg_objectiveText", "Take turns pulling the trigger on a revolver. Hope for the best.");
            player.SpawnedPlayer += () => OnPlayerSpawned(player);
            player.OnNotify("weapon_change", (p, weapon) => OnWeaponChange(p, (string)weapon));
            player.OnNotify("weapon_fired", (p, weapon) => OnWeaponFired(p, (string)weapon));
        }
        private void OnPlayerSpawned(Entity player)
        {
            player.SetClientDvar("ui_allow_classchange", "0");
            player.SetClientDvar("ui_allow_teamchange", "0");
            player.SetClientDvar("ui_hud_hardcore", "0");
            player.SetClientDvar("ui_hud_obituaries", "0");
            player.SetClientDvar("cg_drawCrosshair", "0");
            player.SetClientDvar("cg_objectiveText", "Take turns pulling the trigger on a revolver. Hope for the best.");
            player.SetClientDvar("player_deathInvulnerableToMelee", "1");
            int playerRef = player.EntRef;
            deadPlayers[playerRef] = false;
            player.Call("setorigin", spawns[playerRef]);
            player.Call("freezecontrols", true);
            player.Call("setmovespeedscale", 0);
            player.Call("allowsprint", false);
            player.Call("allowjump", false);
            player.Health = 5;
            player.SetField("maxhealth", 5);
            player.TakeAllWeapons();
            player.AfterDelay(200, p =>
            {
                p.GiveWeapon("bomb_site_mp");
                p.Call("setweaponammoclip", "bomb_site_mp", 999);
                p.Call("setweaponammostock", "bomb_site_mp", 999);
                p.Call(33506);
                p.AfterDelay(300, p2 => p2.SwitchToWeaponImmediate("bomb_site_mp"));
            });
            player.Call(33505);
            if (!modeStarted)
            {
                AfterDelay(2000, () => rotateRoulette());
                modeStarted = true;
            }
            //if (playerRef == currentPlayer) startRouletteForPlayer(player);
        }
        private void OnWeaponChange(Entity player, string weapon)
        {
            player.Call("setmovespeedscale", 0);
            player.Call("setspreadoverride", 0);
            player.Call("recoilscaleon", 50);
            if (weapon == "bomb_site_mp")
            {
                //rotateRoulette();
                player.Call("freezecontrols", true);
            }
        }
        private void OnWeaponFired(Entity player, string weapon)
        {
            if ((string)weapon == "iw5_44magnum_mp") totalShotsTaken = 0;
            player.SwitchToWeapon("bomb_site_mp"); 
            player.AfterDelay(800, (p) => p.TakeWeapon("iw5_44magnum_mp"));
            AfterDelay(2000, () => rotateRoulette());
        }
        private void rotateRoulette()
        {
            currentPlayer++;
            Log.Write(LogLevel.All, "Attempting player {0}", currentPlayer);
            if (currentPlayer > 16) currentPlayer = 0;
            Entity player = Entity.GetEntity(currentPlayer);
            if (player == null || !player.IsPlayer || !player.IsAlive)
            {
                rotateRoulette();
                return;
            }
            startRouletteForPlayer(player);
            Log.Write(LogLevel.All, "{0} picked", player.Name);
            foreach (Entity players in Players)
            {
                if (players.IsAlive & players != player) lerpPlayerViewToPlayer(players, player);
            }
        }
        private void startRouletteForPlayer(Entity player)
        {
            int? roulette = new Random().Next(6);
            bool hasBullet = false;
            switch (roulette)
            {
                case 4:
                    hasBullet = true;
                    break;
                default:
                    hasBullet = false;
                    break;
            }
            player.GiveWeapon("iw5_44magnum_mp");
            player.Call("freezecontrols", false);
            player.Call("setweaponammoclip", "iw5_44magnum_mp", 0);
            player.Call("setweaponammostock", "iw5_44magnum_mp", 0);
            AfterDelay(300, () =>
                {
                    player.Call(33506);
                    player.SwitchToWeapon("iw5_44magnum_mp");
                    player.Call(33505);
                    totalShotsTaken++;
                    if (totalShotsTaken > 5) hasBullet = true;
                    Log.Write(LogLevel.All, "{0} ready to shoot with bullet: {1}", player.Name, hasBullet);
                    if (hasBullet)
                        player.Call("setweaponammoclip", "iw5_44magnum_mp", 1);
                    else
                        player.OnInterval(50, (p) =>
                            {
                                bool pulled = p.Call<int>(33534) == 1;
                                if (pulled)
                                {
                                    OnWeaponFired(p, "");
                                    return false;
                                }
                                else return true;
                            });
                });
        }
        private void lerpPlayerViewToPlayer(Entity player, Entity target)
        {
            Log.Write(LogLevel.All, "Lerping {0}'s view to {1}'s pos", player.Name, target.Name);
            Vector3 targetAngles = Call<Vector3>(247, target.Origin - player.Origin);
            Vector3 playerAngles = player.Call<Vector3>(33532);
            //int time = Call<int>(51);
            float overallTime = 0;
            player.OnInterval(50, (p) =>
                {
                    Vector3 lerpStep = Call<Vector3>(249, playerAngles, targetAngles, overallTime / 3);
                    p.Call(33531, lerpStep);
                    overallTime += .05f;
                    if (overallTime > 1.99f) return false;
                    return true;
                });
        }

        private void SetupPlayspace()
        {
            if (mapname == "mp_dome")
            {
                for (int i = 18; i < 2000; i++)
                {
                    Entity ent = Entity.GetEntity(i);
                    string entModel = ent.GetField<string>("model");
                    if (entModel == "vehicle_hummer_destructible" || entModel.Contains("vehicle") || entModel == "berlin_hesco_barrier_med")
                        ent.Call("delete");
                }
                baseSpawn = new Vector3(-109.0941f, 1645.611f, -290.875f);
                for (int i = 0; i < 16; i++)
                    spawns[i] = baseSpawn + spawnModifiers[i];
            }
        }
        private void SetupHUD()
        {
            timer = HudElem.CreateServerFontString("objective", 2);
            timer.SetPoint("CENTER", "CENTER", 0, -150);
            timer.Archived = true;
            timer.Alpha = 1;
            timer.Color = new Vector3(1, .5f, .5f);
            timer.Foreground = true;
            timer.HideWhenInMenu = true;
            timer.Call("settimerstatic", 20);
        }
    }
}
