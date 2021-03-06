using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Discord;
using Discord.Commands;

namespace SmirkBotv2
{
    class SmirkBot
    {
        bool signupflag = false; //initialize variables
        List<string> signupids = new List<string>();
        List<string> signupnames = new List<string>();
        string eventname;
        string temp;
        string tempnames;
        string remarg;
        string outputindex;
        int ltemp;
        int index = 0;
        int rng = 0;
        bool ss3flag = false;
        bool ingameflag = false;
        int capacity = 0;
        int signupnum = 0;
        string[] list = { "Vanilla Townie", "Super-Saint", "Mafia Goon" };
        string[] nlist = new string[3];
        string[] players = new string[3] { "", "", "" };
        DiscordClient discord;
        public SmirkBot()
        {
            int[] votecounts = new int[3] { 0, 0, 0 };
            int[] votetargets = new int[3] { -1, -1, -1 };
            discord = new DiscordClient(x =>
           {
               x.LogLevel = LogSeverity.Info;
               x.LogHandler = Log;
           });
            discord.UsingCommands(x =>
            {
                x.PrefixChar = '!';
                x.AllowMentionPrefix = true;
            });
            var commands = discord.GetService<CommandService>();
            
            //Signups
            commands.CreateCommand("create").Parameter("event", ParameterType.Multiple) //create an event
            .Do(async (e) =>
            {
                if(signupflag==false)
                {
                    signupflag = true;
                    for (int i = 0; i < e.Args.Length; i++)
                    {
                        eventname += e.Args[i] + ' ';
                    }
                    eventname.Remove(eventname.Length - 1);
                    await e.Channel.SendMessage("An event named " + eventname + "has been created.");
                }
                else
                {
                    await e.Channel.SendMessage("An event has already been created.");
                }
                
            });
            commands.CreateCommand("signup").Parameter("name", ParameterType.Optional) //allows someone to sign up for an event
            .Do(async (e) =>
            {
                if(signupflag==true)
                {
                    for (int i = 0; i < e.Args.Length; i++)
                    {
                        temp += e.Args[i] + ' ';
                    }
                    if(temp == " ")
                    {
                        await e.Channel.SendMessage(e.User.ToString() + " has been added to the signups list!");
                    }
                    else
                    {
                        await e.Channel.SendMessage(temp + "(" + e.User.ToString() + ") has been added to the signups list!");
                    }
                    addto(signupids, e.User.ToString());
                    
                    if(temp==" ")
                    {
                        addto(signupnames, e.User.Name);
                    }
                    else
                    {
                        temp.Remove(temp.Length - 1);
                        addto(signupnames, temp);
                    }
                    temp = ""; 
                }
            });
            commands.CreateCommand("list") //allows someone to check the list of signups
            .Do(async (e) =>
            {
                if (signupflag == true)
                {
                    if (signupids.Count == 0)
                    {
                        await e.Channel.SendMessage("Nobody has signed up for this event yet.");
                    }
                    else
                    {
                        for (int i = 0; i < signupids.Count; i++)
                        {
                            ltemp = i + 1;
                            outputindex = ltemp.ToString();
                            tempnames += ltemp + " | " + signupnames[i] + " (" + signupids[i] + ")\n";
                        }
                        await e.Channel.SendMessage("Signups for " + eventname + ":```" + tempnames + "```");
                    }
                }
                tempnames = "";
            });
            commands.CreateCommand("remove").Parameter("num", ParameterType.Required) //allows someone to remove themselves from the signups, or someone else if they have ban permission
           .Do(async (e) =>
           {
               if(signupflag==true)
               {
                   remarg = e.GetArg("num");
                   index = Convert.ToInt32(remarg) - 1;
                   if (e.User.ServerPermissions.BanMembers == true || e.User.ToString().Equals(signupids[index]) == true)
                   {
                       signupids.RemoveAt(index);
                       signupnames.RemoveAt(index);
                       await e.Channel.SendMessage("Signup deleted successfully.");
                   }
                   else
                   {
                       await e.Channel.SendMessage("You do not have permission to do that.");
                   }
              } 
           });
            commands.CreateCommand("erase") //clears the entire list of signup, including the event name
           .Do(async (e) =>
           {
               if(signupflag==true && e.User.ServerPermissions.BanMembers == true)
               {
                   signupids = new List<string>();
                   signupnames = new List<string>();
                   await e.Channel.SendMessage("Signups cleared!");
                   signupflag = false;
               }
           });
            commands.CreateCommand("dice").Parameter("num", ParameterType.Required) //rolls a dice
            .Do(async (e) =>
            {
                remarg = e.GetArg("num");
                rng = Convert.ToInt32(remarg);
                rng = RandomNumber(1, rng);
                await e.Channel.SendMessage("```The dice roll is " + rng + "!```");
            });
            //Mafia
            commands.CreateCommand("ss3") //creates new game of SS3
          .Do(async (e) =>
          {
              if (ss3flag == false)
              {
                  await e.Channel.SendMessage("Starting a game of ss3. Type ```!in``` to enter.");
                  ss3flag = true;
                  capacity = 3;
                  randomizeRoles();
                   //await e.Channel.SendMessage("Roles randomized: " + list[0] + " " + list[1] + " " + list[2]); - use this if testing
               }

          });
            commands.CreateCommand("in") //adds user to signup list
            .Do(async (e) =>
            {
                if (ss3flag == true)
                {
                    if (preventMultipleSignups(e.User.Name) == false)
                    {
                        await e.Channel.SendMessage("You have already signed up for this game.");
                    }
                    else if (capacity == signupnum)
                    {
                        await e.Channel.SendMessage("The capacity for this game has already been reached. I'm afraid you will have to be an observer for this game.");
                    }
                    else
                    {
                        players[signupnum] = e.User.Name;
                        await e.Channel.SendMessage(e.User.Name + " has been added to the signups list! " + e.User.Name + ", please check your inbox for your role PM.");
                        await e.User.SendMessage(e.User.Name + ", you are the " + list[signupnum] + ". Please do not talk about the game until the game has started.");
                        signupnum++;
                    }
                }
            });
            commands.CreateCommand("start") //starts the game if three people have signed up
            .Do(async (e) =>
            {
                if (ss3flag == true)
                {
                    if (capacity == signupnum)
                    {
                        await e.Channel.SendMessage("It is now Day 1. With 3 people alive it takes 2 people to lynch. Good luck!");
                        ingameflag = true;
                        await e.Channel.SendMessage("Playerlist: ");
                        string temp = "```";
                        for (int i = 0; i < 3; i++)
                        {
                            int f = i + 1;
                            string x = f.ToString();
                            temp += x + " " + players[i] + "\n";

                        }
                        temp += "```";
                        await e.Channel.SendMessage(temp);
                    }
                    else
                    {
                        await e.Channel.SendMessage("Error: There are not enough people to start this game.");
                    }
                }
            });
            commands.CreateCommand("vtl").Parameter("num", ParameterType.Required) //records VTL of a user
            .Do(async (e) =>
            {
                int x = StringToInt(e.GetArg("num"));
                int z = findIndex(e.User.Name);
                if (ingameflag == true && z != -1 && votetargets[z] == -1)
                {
                    if (x >= 1 && x <= 3)
                    {
                        votecounts[x - 1]++;
                        votetargets[z] = x - 1;
                        await e.Channel.SendMessage(e.User.Name + " has voted " + players[x - 1] + '.');
                        if (votecounts[x - 1] == 2)
                        {
                            await e.Channel.SendMessage(players[x - 1] + " has been lynched and is revealed to be the " + list[x - 1] + "!");
                            if (list[x - 1].Equals("Super-Saint") == true)
                            {
                                await e.Channel.SendMessage("The Super-Saint has avenged his death by killing " + players[z] + "! " + e.User.Name + " was a " + list[z] + '!');
                                if (list[z] == "Mafia Goon")
                                {
                                    await e.Channel.SendMessage("The Town Won!");
                                }
                                else
                                {
                                    await e.Channel.SendMessage("The Mafia Won!");
                                }
                            }
                            else if (list[x - 1].Equals("Mafia Goon") == true)
                            {
                                await e.Channel.SendMessage("The Town Won!");
                            }
                            else
                            {
                                await e.Channel.SendMessage("The Mafia Won!");
                            }
                            for (int i = 0; i < 3; i++)
                            {
                                players[i] = "";
                                nlist = new string[3];
                                votetargets[i] = -1;
                                votecounts[i] = 0;
                                capacity = 0;
                                signupnum = 0;
                            }
                            ss3flag = false;
                            ingameflag = false;
                        }
                    }
                    else
                    {
                        await e.Channel.SendMessage("Invalid.");
                    }
                }
            });
            commands.CreateCommand("unvtl") //allows a user to unVTL
           .Do(async (e) =>
           {
               int j = findIndex(e.User.Name);
               if (ingameflag == true && j != -1 && votetargets[j] != -1)
               {
                   await e.Channel.SendMessage(e.User.Name + " has removed their vote on " + players[votetargets[j]]);
                   votecounts[votetargets[j]]--;
                   votetargets[j] = -1;
               }
               else if (ingameflag == true && votetargets[j] != -1)
               {
                   await e.Channel.SendMessage("You have not voted yet, " + e.User.Name + ".");
               }
           });
            commands.CreateCommand("rules") //outputs the rules
               .Do(async (e) =>
               {
                   await e.Channel.SendMessage("```1. Do not copy-paste your role PM or ask others to do so.\n2. You must play to win at all points in the game.\n3. Do not use DMs unless SmirkBot says so. Spectators should not comment on games or coach others, publicly or privately.```");
                   await e.User.SendMessage("```1. Do not copy-paste your role PM or ask others to do so.\n2. You must play to win at all points in the game.\n3. Do not use DMs unless SmirkBot says so. Spectators should not comment on games or coach others, publicly or privately.```");
               });
            commands.CreateCommand("players") //outputs the list of players 
              .Do(async (e) =>
              {
                  string temp = "```";
                  for (int i = 0; i < 3; i++)
                  {
                      int f = i + 1;
                      string x = f.ToString();
                      temp += x + " " + players[i] + "\n";

                  }
                  temp += "```";
                  await e.Channel.SendMessage(temp);
              });

            commands.CreateCommand("help") //sends a list of commands
               .Do(async (e) =>
               {
                   await e.Channel.SendMessage("Command list sent!");
                   await e.User.SendMessage("```This bot has two specific parts: it can automate signups for an event and/or fully automate a game of SS3 Mafia.\n------------------" +
                       "\nSignups\n!create - creates a signup list\n!signup - adds yourself to the list\n!list - outputs the list of people who have signed up\n!remove - removes signup (must specify index that you want to remove)\n!erase - clears the entire list (requires permissions)\n------------------" + 
                       "\nMafia\n!ss3 - creates a game of Mafia\n!in - allows player to enter and receive role PM\n!start - starts the game (if enough people have signed up)\n!vtl - VTLs a player (specify index of player on the playerlist to be VTL'd, not the username proper)\n!unvtl - UnVTLs a player.\n!players - posts the list of players in the chat\n!rules - posts list of general rules" +
                       "\n------------------\n!dice (number) - rolls a die with (number) of sides\n\nCreated by Radiating: Contact Radiating#4188 for any questions or issues.```");

               });

            discord.ExecuteAndWait(async () =>
            {
                await discord.Connect("Mjc1ODEyMjY2Mzc5NTc1Mjk2.C3GN7Q.2mSH2YX40e80k8mbzHee_heEL5M", TokenType.Bot);
            });
        }

        private int RandomNumber(int min, int max)
        {
            Random random = new Random();
            return random.Next(min, max);
        }
        private void addto(List<string> names, string username)
        {
            names.Add(username);
        }
        private void randomizeRoles() //randomizes role (note: uses global list rather than argument passed through method)
        {
            for (int i = 0; i < list.Length; i++)
            {
                int y = list.Length - i - 1;
                if (y <= 0)
                {
                    nlist[list.Length - 1] = list[0];
                    break;
                }
                else
                {
                    int x = RandomInteger(0, y);
                    nlist[i] = list[x];
                    list[x] = list[y];
                }
            }
            for (int i = 0; i < list.Length; i++)
                list[i] = nlist[i];
        }
        private int RandomInteger(int min, int max) //returns random integer between min and mix
        {
            RNGCryptoServiceProvider Rand = new RNGCryptoServiceProvider();
            uint scale = uint.MaxValue;
            while (scale == uint.MaxValue)
            {
                byte[] four_bytes = new byte[4];
                Rand.GetBytes(four_bytes);
                scale = BitConverter.ToUInt32(four_bytes, 0);
            }
            return (int)(min + (max - min) *
                (scale / (double)uint.MaxValue));
        }
        private int StringToInt(string input)
        {
            return Convert.ToInt32(input);
        }
        private bool preventMultipleSignups(string name) //checks if user is on playerlist
        {
            bool flag = true;
            for (int i = 0; i < signupnum; i++)
            {
                if (name.Equals(players[i]) == true)
                {
                    flag = false;
                }
            }
            return flag;
        }
        int findIndex(string name) //finds index of player on playerlist; returns -1 if user is not found
        {
            for (int i = 0; i < 3; i++)
            {
                if (name == players[i])
                    return i;
            }
            return -1;
        }
        private void Log(object sender, LogMessageEventArgs e)
        {
            Console.WriteLine(e.Message);
        }
    }
}
