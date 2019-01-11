using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Client.Events;
using Discord.WebSocket;

namespace Twitch_Discord_Reward_Bot.Backend.Bots.Commands
{
    using Objects = Data.APIIntergrations.RewardCurrencyAPI.Objects;
    public class CommandHandler : BaseObject
    {
        public CommandHandler(BotInstance BotInstance) : base(BotInstance) { }


        public void Handle(object sender, OnMessageReceivedArgs e)
        {
            Handle(sender, StandardisedMessageRequest.FromTwitch(e));
        }
        public async Task Handle(SocketMessage e)
        {
            Handle(null, StandardisedMessageRequest.FromDiscord(e));
        }

        public void Handle(object sender, StandardisedMessageRequest e)
        {
            new Thread(async () => await HandleThread(e)).Start();
        }

        async Task HandleThread(StandardisedMessageRequest e)
        {
            try
            {
                if (e.SenderID != BotInstance.DiscordBot.Client.CurrentUser.Id.ToString())
                {
                    string Prefix = BotInstance.CommandConfig["Prefix"].ToString(),
                        Command = e.SegmentedBody[0].Replace(Prefix, "").ToLower();

                    if (e.MessageType == MessageType.Discord && BotInstance.CommandConfig["Discord"]["Channels"].Where(x => x.ToString() == e.ChannelID).Count() == 0) { return; }

                    if (e.SegmentedBody[0].StartsWith(Prefix) && !e.SegmentedBody[0].StartsWith(Prefix + Prefix))
                    {
                        Objects.Bank.MergeAccounts(e, BotInstance, e.SenderID);
                        if (CommandEnabled(BotInstance.CommandConfig["CommandSetup"]["Balance"], e) &&
                            JArrayContainsString(BotInstance.CommandConfig["CommandSetup"]["Balance"]["Commands"], Command))
                        {
                            if (LiveCheck(BotInstance.CommandConfig["CommandSetup"]["Balance"]))
                            {
                                if (e.SegmentedBody.Length == 1)
                                {
                                    Objects.Bank B = Objects.Bank.FromTwitchDiscord(e, BotInstance, e.SenderID);
                                    if (B != null)
                                    {
                                        await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Balance"]["Responses"]["OwnBalance"].ToString(), e, null, B.Balance);
                                    }
                                    else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["APIError"].ToString(), e); }
                                }
                                else if (e.SegmentedBody.Length == 2)
                                {
                                    StandardisedUser U = IDFromMessageSegment(e.SegmentedBody[1], e);
                                    if (U.ID != null)
                                    {
                                        Objects.Bank B = Objects.Bank.FromTwitchDiscord(e, BotInstance, U.ID);
                                        if (B != null)
                                        {
                                            await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Balance"]["Responses"]["OtherBalance"].ToString(), e, U, B.Balance);
                                        }
                                        else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["APIError"].ToString(), e); }
                                    }
                                    else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["CannotFindUser"].ToString(), e); }
                                }
                                else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["ParamaterCount"].ToString(), e); }
                            }
                        }
                        else if (CommandEnabled(BotInstance.CommandConfig["CommandSetup"]["Pay"], e) &&
                            JArrayContainsString(BotInstance.CommandConfig["CommandSetup"]["Pay"]["Commands"], Command))
                        {
                            if (LiveCheck(BotInstance.CommandConfig["CommandSetup"]["Pay"]))
                            {
                                if (e.SegmentedBody.Length == 3)
                                {
                                    StandardisedUser U = IDFromMessageSegment(e.SegmentedBody[1], e);
                                    if (U.ID != null)
                                    {
                                        Objects.Bank Self = Objects.Bank.FromTwitchDiscord(e, BotInstance, e.SenderID),
                                            Other = Objects.Bank.FromTwitchDiscord(e, BotInstance, U.ID);
                                        int ChangeBy = ValueFromMessageSegment(e.SegmentedBody[2], Self),
                                            MinPayment = int.Parse(BotInstance.CommandConfig["CommandSetup"]["Pay"]["MinimumPayment"].ToString());
                                        if (ChangeBy == -1) { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["NumberParamaterInvalid"].ToString(), e); return; }
                                        if (Self != null && Other != null)
                                        {
                                            if (ChangeBy >= MinPayment)
                                            {
                                                if (ChangeBy >= 0)
                                                {
                                                    if (Self.Balance - ChangeBy >= 0)
                                                    {
                                                        if (Objects.Bank.AdjustBalance(Self, ChangeBy, "-"))
                                                        {
                                                            if (Objects.Bank.AdjustBalance(Other, ChangeBy, "+"))
                                                            {
                                                                await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Pay"]["Responses"]["Paid"].ToString(), e, U, ChangeBy);
                                                            }
                                                            else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["APIError"].ToString(), e); }
                                                        }
                                                        else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["APIError"].ToString(), e); }
                                                    }
                                                    else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Pay"]["Responses"]["NotEnough"].ToString(), e); }
                                                }
                                                else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["NumberParamaterNegative"].ToString(), e); }
                                            }
                                            else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Pay"]["Responses"]["TooSmall"].ToString(), e, null, MinPayment); }
                                        }
                                        else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["APIError"].ToString(), e); }
                                    }
                                    else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["CannotFindUser"].ToString(), e); }
                                }
                                else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["ParamaterCount"].ToString(), e); }
                            }
                        }
                        else if (CommandEnabled(BotInstance.CommandConfig["CommandSetup"]["Gamble"], e) &&
                            JArrayContainsString(BotInstance.CommandConfig["CommandSetup"]["Gamble"]["Commands"], Command))
                        {
                            if (LiveCheck(BotInstance.CommandConfig["CommandSetup"]["Gamble"]))
                            {
                                if (e.SegmentedBody.Length == 2)
                                {
                                    Objects.Bank Self = Objects.Bank.FromTwitchDiscord(e, BotInstance, e.SenderID);
                                    int ChangeBy = ValueFromMessageSegment(e.SegmentedBody[1], Self),
                                           MinPayment = int.Parse(BotInstance.CommandConfig["CommandSetup"]["Gamble"]["MinimumPayment"].ToString()),
                                           WinChance = int.Parse(BotInstance.CommandConfig["CommandSetup"]["Gamble"]["WinChance"].ToString()),
                                           WinMultiplyer = int.Parse(BotInstance.CommandConfig["CommandSetup"]["Gamble"]["WinMultiplyer"].ToString());
                                    if (ChangeBy == -1) { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["NumberParamaterInvalid"].ToString(), e); return; }
                                    if (Self != null)
                                    {
                                        if (ChangeBy >= MinPayment)
                                        {
                                            if (ChangeBy >= 0)
                                            {
                                                if (Self.Balance - ChangeBy >= 0)
                                                {
                                                    string Operator;
                                                    if (Init.Rnd.Next(0, 100) <= WinChance) { Operator = "+"; ChangeBy *= WinMultiplyer; }
                                                    else { Operator = "-"; }
                                                    if (Objects.Bank.AdjustBalance(Self, ChangeBy, Operator))
                                                    {
                                                        if (Operator == "+") { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Gamble"]["Responses"]["Win"].ToString(), e, null, ChangeBy); }
                                                        else if (Operator == "-") { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Gamble"]["Responses"]["Lose"].ToString(), e, null, ChangeBy); }
                                                    }
                                                    else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["APIError"].ToString(), e); }
                                                }
                                                else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Pay"]["Responses"]["NotEnough"].ToString(), e); }
                                            }
                                            else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["NumberParamaterNegative"].ToString(), e); }
                                        }
                                        else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Pay"]["Responses"]["TooSmall"].ToString(), e, null, MinPayment); }
                                    }
                                    else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["APIError"].ToString(), e); }
                                }
                                else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["ParamaterCount"].ToString(), e); }
                            }
                        }
                        else if (CommandEnabled(BotInstance.CommandConfig["CommandSetup"]["Slots"], e) &&
                            JArrayContainsString(BotInstance.CommandConfig["CommandSetup"]["Slots"]["Commands"], Command))
                        {
                            if (LiveCheck(BotInstance.CommandConfig["CommandSetup"]["Slots"]))
                            {
                                if (e.SegmentedBody.Length == 2)
                                {
                                    Objects.Bank Self = Objects.Bank.FromTwitchDiscord(e, BotInstance, e.SenderID);
                                    int ChangeBy = ValueFromMessageSegment(e.SegmentedBody[1], Self),
                                        MinPayment = int.Parse(BotInstance.CommandConfig["CommandSetup"]["Slots"]["MinimumPayment"].ToString()),
                                        WinChance = int.Parse(BotInstance.CommandConfig["CommandSetup"]["Slots"]["WinChance"].ToString()),
                                        WinMultiplyer = int.Parse(BotInstance.CommandConfig["CommandSetup"]["Slots"]["WinMultiplyer"].ToString());
                                    if (ChangeBy == -1) { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["NumberParamaterInvalid"].ToString(), e); return; }
                                    if (Self != null)
                                    {
                                        if (ChangeBy >= MinPayment)
                                        {
                                            if (ChangeBy >= 0)
                                            {
                                                if (Self.Balance - ChangeBy >= 0)
                                                {
                                                    string Operator;
                                                    if (Init.Rnd.Next(0, 100) <= WinChance) { Operator = "+"; ChangeBy *= WinMultiplyer; }
                                                    else { Operator = "-"; }
                                                    if (Objects.Bank.AdjustBalance(Self, ChangeBy, Operator))
                                                    {
                                                        Newtonsoft.Json.Linq.JToken EmoteSet = null;
                                                        if (e.MessageType == MessageType.Discord) { EmoteSet = BotInstance.CommandConfig["CommandSetup"]["Slots"]["Emotes"]["Discord"]; }
                                                        if (e.MessageType == MessageType.Twitch) { EmoteSet = BotInstance.CommandConfig["CommandSetup"]["Slots"]["Emotes"]["Twitch"]; }
                                                        if (Operator == "+")
                                                        {
                                                            int i = Init.Rnd.Next(0, EmoteSet.Count());
                                                            string PanelString = "[ " + EmoteSet[i].ToString() + " | " + EmoteSet[i].ToString() + " | " + EmoteSet[i].ToString() + " ]";
                                                            await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Slots"]["Responses"]["Win"].ToString(), e, null, ChangeBy, -1, PanelString);
                                                        }
                                                        else if (Operator == "-")
                                                        {
                                                            string[] PanelArray = new string[] { "", "", "" };
                                                            while (PanelArray[0] == PanelArray[1] && PanelArray[1] == PanelArray[2])
                                                            {
                                                                PanelArray = new string[] { EmoteSet[Init.Rnd.Next(0, EmoteSet.Count())].ToString(), EmoteSet[Init.Rnd.Next(0, EmoteSet.Count())].ToString(), EmoteSet[Init.Rnd.Next(0, EmoteSet.Count())].ToString() };
                                                            }
                                                            await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Slots"]["Responses"]["Lose"].ToString(), e, null, ChangeBy, -1, "[ " + PanelArray[0] + " | " + PanelArray[1] + " | " + PanelArray[2] + " ]");
                                                        }
                                                    }
                                                    else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["APIError"].ToString(), e); }
                                                }
                                                else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Pay"]["Responses"]["NotEnough"].ToString(), e); }
                                            }
                                            else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["NumberParamaterNegative"].ToString(), e); }
                                        }
                                        else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Pay"]["Responses"]["TooSmall"].ToString(), e, null, MinPayment); }
                                    }
                                    else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["APIError"].ToString(), e); }
                                }
                                else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["ParamaterCount"].ToString(), e); }
                            }
                        }
                        else if (CommandEnabled(BotInstance.CommandConfig["CommandSetup"]["Fish"], e) &&
                            JArrayContainsString(BotInstance.CommandConfig["CommandSetup"]["Fish"]["Commands"], Command))
                        {
                            if (LiveCheck(BotInstance.CommandConfig["CommandSetup"]["Fish"]))
                            {
                                if (e.SegmentedBody.Length == 1)
                                {
                                    Objects.Bank Self = Objects.Bank.FromTwitchDiscord(e, BotInstance, e.SenderID);
                                    int ViewerCost = int.Parse(BotInstance.CommandConfig["CommandSetup"]["Fish"]["Cost"]["Viewer"].ToString()),
                                        SubscriberCost = int.Parse(BotInstance.CommandConfig["CommandSetup"]["Fish"]["Cost"]["Subscriber"].ToString());
                                    int Cost = ViewerCost;
                                    if (Self != null)
                                    {
                                        if (e.MessageType == MessageType.Twitch)
                                        { if (e.TwitchRaw.ChatMessage.IsSubscriber) { Cost = SubscriberCost; } }
                                        if (e.MessageType == MessageType.Discord)
                                        { if (((SocketGuildUser)e.DiscordRaw.Author).Roles.Where(x => x.Id.ToString() == BotInstance.CommandConfig["Discord"]["SubscriberRoleID"].ToString()).Count() != 0) { Cost = SubscriberCost; } }
                                        if (Self.Balance - Cost >= 0)
                                        {
                                            if (BotInstance.TimeEvents.Fishermen.Where(x => x.Value.e.SenderID == e.SenderID).Count() == 0)
                                            {
                                                if (Objects.Bank.AdjustBalance(Self, Cost, "-"))
                                                {
                                                    await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Fish"]["Responses"]["Started"].ToString(), e);
                                                    BotInstance.TimeEvents.Fishermen.Add(DateTime.Now, new Fisherman(e, BotInstance));
                                                }
                                                else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["APIError"].ToString(), e); }
                                            }
                                            else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Fish"]["Responses"]["AlreadyFishing"].ToString(), e); }
                                        }
                                        else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Pay"]["Responses"]["NotEnough"].ToString(), e); }
                                    }
                                    else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["APIError"].ToString(), e); }
                                }
                                else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["ParamaterCount"].ToString(), e); }
                            }
                        }
                        else if (CommandEnabled(BotInstance.CommandConfig["CommandSetup"]["Duel"], e) &&
                            JArrayContainsString(BotInstance.CommandConfig["CommandSetup"]["Duel"]["Commands"], Command))
                        {
                            if (LiveCheck(BotInstance.CommandConfig["CommandSetup"]["Duel"]))
                            {
                                if (e.SegmentedBody.Length == 3)
                                {
                                    int MinimumPayment = int.Parse(BotInstance.CommandConfig["CommandSetup"]["Duel"]["MinimumPayment"].ToString());
                                    StandardisedUser Target = IDFromMessageSegment(e.SegmentedBody[1], e);
                                    if (Target != null)
                                    {
                                        Objects.Bank Self = Objects.Bank.FromTwitchDiscord(e, BotInstance, e.SenderID),
                                            TargetBank = Objects.Bank.FromTwitchDiscord(e, BotInstance, Target.ID);
                                        if (Self != null && TargetBank != null)
                                        {
                                            int ChangeBy = ValueFromMessageSegment(e.SegmentedBody[2], Self),
                                                TargetChangeBy = ValueFromMessageSegment(e.SegmentedBody[2], TargetBank);
                                            if (ChangeBy != -1 && TargetChangeBy != -1)
                                            {
                                                if (TargetChangeBy < ChangeBy) { ChangeBy = TargetChangeBy; }
                                                if (ChangeBy >= MinimumPayment)
                                                {
                                                    if (ChangeBy <= Self.Balance)
                                                    {
                                                        if (ChangeBy <= TargetBank.Balance)
                                                        {
                                                            Duel Duel = new Duel();
                                                            Duel.BotInstance = BotInstance;
                                                            StandardisedUser S = new StandardisedUser();
                                                            S.ID = e.SenderID; S.UserName = e.SenderUserName;
                                                            Duel.Creator = S; Duel.Acceptor = Target;
                                                            if (!BotInstance.TimeEvents.UserDueling(S))
                                                            {
                                                                if (!BotInstance.TimeEvents.UserDueling(Target))
                                                                {
                                                                    Duel.e = e;
                                                                    Duel.ChangeBy = ChangeBy;
                                                                    BotInstance.TimeEvents.UserDueling(S);
                                                                    BotInstance.TimeEvents.Duels.Add(DateTime.Now, Duel);
                                                                    await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Duel"]["Responses"]["Started"].ToString(), e, Target, ChangeBy);
                                                                }
                                                                else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Duel"]["Responses"]["OtherDueling"].ToString(), e, Target); }
                                                            }
                                                            else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Duel"]["Responses"]["SelfDueling"].ToString(), e); }
                                                        }
                                                        else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Duel"]["Responses"]["OtherNotEnough"].ToString(), e, Target); }
                                                    }
                                                    else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Duel"]["Responses"]["SelfNotEnough"].ToString(), e); }
                                                }
                                                else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Duel"]["Responses"]["TooSmall"].ToString(), e, null, MinimumPayment); }
                                            }
                                            else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["NumberParamaterInvalid"].ToString(), e); return; }
                                        }
                                        else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["APIError"].ToString(), e); }
                                    }
                                    else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["CannotFindUser"].ToString(), e); }
                                }
                                else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["ParamaterCount"].ToString(), e); }
                            }
                        }
                        else if (CommandEnabled(BotInstance.CommandConfig["CommandSetup"]["Duel"], e) &&
                            JArrayContainsString(BotInstance.CommandConfig["CommandSetup"]["Duel"]["Accepting"]["Commands"], Command))
                        {
                            StandardisedUser U = new StandardisedUser();
                            U.ID = e.SenderID; U.UserName = e.SenderUserName;
                            if (BotInstance.TimeEvents.UserDueling(U))
                            {
                                KeyValuePair<DateTime, Duel> TDuel = BotInstance.TimeEvents.Duels.Where(x => x.Value.Acceptor.ID == U.ID || x.Value.Creator.ID == U.ID).First();
                                BotInstance.TimeEvents.Duels.Remove(TDuel.Key);
                                Objects.Bank Acceptor = Objects.Bank.FromTwitchDiscord(e, BotInstance, TDuel.Value.Acceptor.ID),
                                    Creator = Objects.Bank.FromTwitchDiscord(e, BotInstance, TDuel.Value.Creator.ID);
                                if (Acceptor != null && Creator != null)
                                {
                                    if (TDuel.Value.ChangeBy <= Acceptor.Balance)
                                    {
                                        if (TDuel.Value.ChangeBy <= Creator.Balance)
                                        {
                                            int Winner = Init.Rnd.Next(0, 2);
                                            if (Winner == 0)
                                            {
                                                Objects.Bank.AdjustBalance(Acceptor, TDuel.Value.ChangeBy, "+");
                                                Objects.Bank.AdjustBalance(Creator, TDuel.Value.ChangeBy, "-");
                                                await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Duel"]["Accepting"]["Responses"]["Win"].ToString(), e, TDuel.Value.Creator,TDuel.Value.ChangeBy);
                                            }
                                            if (Winner == 1)
                                            {
                                                Objects.Bank.AdjustBalance(Acceptor, TDuel.Value.ChangeBy, "-");
                                                Objects.Bank.AdjustBalance(Creator, TDuel.Value.ChangeBy, "+");
                                                await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Duel"]["Accepting"]["Responses"]["Lose"].ToString(), e, TDuel.Value.Creator, TDuel.Value.ChangeBy);
                                            }
                                        }
                                        else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Duel"]["Accepting"]["Responses"]["OtherNotEnough"].ToString(), e, TDuel.Value.Creator); }
                                    }
                                    else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Duel"]["Accepting"]["Responses"]["SelfNotEnough"].ToString(), e); }
                                }
                                else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["APIError"].ToString(), e); }
                            }
                            else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Duel"]["Accepting"]["Responses"]["NotDueling"].ToString(), e); }
                        }
                        else if (CommandEnabled(BotInstance.CommandConfig["CommandSetup"]["Duel"], e) &&
                            JArrayContainsString(BotInstance.CommandConfig["CommandSetup"]["Duel"]["Denying"]["Commands"], Command))
                        {
                            StandardisedUser U = new StandardisedUser();
                            U.ID = e.SenderID; U.UserName = e.SenderUserName;
                            if (BotInstance.TimeEvents.UserDueling(U))
                            {
                                KeyValuePair<DateTime, Duel> TDuel = BotInstance.TimeEvents.Duels.Where(x => x.Value.Acceptor.ID == U.ID || x.Value.Creator.ID == U.ID).First();
                                BotInstance.TimeEvents.Duels.Remove(TDuel.Key);
                                if (e.SenderID == TDuel.Value.Acceptor.ID) { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Duel"]["Denying"]["Responses"]["Canceled"].ToString(), e, TDuel.Value.Creator); }
                                if (e.SenderID == TDuel.Value.Creator.ID) { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Duel"]["Denying"]["Responses"]["Canceled"].ToString(), e, TDuel.Value.Acceptor); }
                            }
                            else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Duel"]["Denying"]["Responses"]["NotDueling"].ToString(), e); }
                        }
                        else if (CommandEnabled(BotInstance.CommandConfig["Raffle"],e)&&
                            JArrayContainsString(BotInstance.CommandConfig["Raffle"]["Joining"]["Commands"],Command))
                        {
                            StandardisedUser U = new StandardisedUser();
                            U.ID = e.SenderID; U.UserName = e.SenderUserName;
                            if (!BotInstance.TimeEvents.UserRaffleing(U))
                            {
                                Raffler R = new Raffler();
                                R.User = U; R.RequestedFrom = e.MessageType;
                                BotInstance.TimeEvents.RaffleParticipants.Add(R);
                                if (BotInstance.CommandConfig["Raffle"]["Joining"]["Responses"]["Joined"].ToString() != "") { await SendMessage(BotInstance.CommandConfig["Raffle"]["Joining"]["Responses"]["Joined"].ToString(), e); }
                            }
                            else if (BotInstance.CommandConfig["Raffle"]["Joining"]["Responses"]["AlreadyRaffling"].ToString() != "")
                            {
                                await SendMessage(BotInstance.CommandConfig["Raffle"]["Joining"]["Responses"]["AlreadyRaffling"].ToString(), e);
                            }
                        }
                        else if (CommandEnabled(BotInstance.CommandConfig["CommandSetup"]["SimpleResponses"], e) &&
                            BotInstance.CommandConfig["CommandSetup"]["SimpleResponses"]["Commands"][Command.ToLower()] != null)
                        {
                            if (LiveCheck(BotInstance.CommandConfig["CommandSetup"]["SimpleResponses"]))
                            {
                                await SendMessage(BotInstance.CommandConfig["CommandSetup"]["SimpleResponses"]["Commands"][Command.ToLower()].ToString(), e);
                            }
                        }
                        else if (CommandEnabled(BotInstance.CommandConfig["CommandSetup"]["FallbackMessage"], e))
                        {
                            await SendMessage(BotInstance.CommandConfig["CommandSetup"]["FallbackMessage"]["Response"].ToString(), e);
                        }
                    }
                }
            }
            catch (Exception E) { Console.WriteLine(E); }
        }

        public StandardisedUser IDFromMessageSegment(string MessageSegment, StandardisedMessageRequest e)
        {
            if (e.MessageType == MessageType.Discord)
            {
                return StandardisedUser.FromDiscordMention(MessageSegment, BotInstance);
            }
            else if (e.MessageType == MessageType.Twitch)
            {
                StandardisedUser S = StandardisedUser.FromTwitchUsername(MessageSegment,BotInstance);
                return S;
            }
            return null;
        }

        public int ValueFromMessageSegment(string MessageSegment,Objects.Bank Bank)
        {
            try { return int.Parse(MessageSegment); } catch { }
            if (MessageSegment.ToLower() == "all") { return Bank.Balance; }
            if (MessageSegment.ToLower().EndsWith("k"))
            {
                try { return int.Parse(MessageSegment.ToLower().Replace("k", "")) * 1000; } catch { }
            }
            if (MessageSegment.ToLower().EndsWith("m"))
            {
                try { return int.Parse(MessageSegment.ToLower().Replace("m", "")) * 1000000; } catch { }
            }
            return -1;
        }

        public bool JArrayContainsString(Newtonsoft.Json.Linq.JToken Array,string S)
        {
            foreach (Newtonsoft.Json.Linq.JToken Item in Array)
            {
                if (Item.ToString() == S) { return true; }
            }
            return false;
        }

        public bool CommandEnabled(Newtonsoft.Json.Linq.JToken Command,StandardisedMessageRequest e)
        {
            return CommandEnabled(Command, e.MessageType);
        }
        public bool CommandEnabled(Newtonsoft.Json.Linq.JToken Command, MessageType e)
        {
            if (e == MessageType.Discord)
            {
                if (Command["DiscordEnabled"].ToString().ToLower() == "true") { return true; }
            }
            if (e == MessageType.Twitch)
            {
                if (Command["TwitchEnabled"].ToString().ToLower() == "true") { return true; }
            }
            return false;
        }

        public bool LiveCheck(Newtonsoft.Json.Linq.JToken Object)
        {
            if (Object["RequireLive"].ToString().ToLower() == "true")
            {
                return Data.APIIntergrations.Twitch.IsLive(BotInstance);
            }
            return true;
        }

        public async Task SendMessage(string ParamaterisedMessage, StandardisedMessageRequest e, StandardisedUser TargetUser = null, int Amount = -1, int NewBal = -1, string OtherString = "", string SenderUsername = null)
        {
            ParamaterisedMessage = MessageParser(ParamaterisedMessage, e, e.MessageType,TargetUser, Amount, NewBal, OtherString, SenderUsername);

            if (e.MessageType == MessageType.Twitch)
            {
                BotInstance.TwitchBot.Client.SendMessage(e.ChannelName, ParamaterisedMessage);
            }
            else
            {
                await e.DiscordRaw.Channel.SendMessageAsync(ParamaterisedMessage);
            }
        }

        public async Task SendMessage(string ParamaterisedMessage, string Channel, MessageType MessageType, StandardisedUser TargetUser = null, int Amount = -1, int NewBal = -1, string OtherString = "", string SenderUsername = null)
        {
            ParamaterisedMessage = MessageParser(ParamaterisedMessage, null, MessageType, TargetUser, Amount, NewBal, OtherString, SenderUsername);

            if (MessageType == MessageType.Twitch)
            {
                BotInstance.TwitchBot.Client.SendMessage(Channel, ParamaterisedMessage);
            }
            else
            {
                await ((ISocketMessageChannel)BotInstance.DiscordBot.Client.GetChannel(ulong.Parse(Channel))).SendMessageAsync(ParamaterisedMessage);
            }
        }

        public string MessageParser(string ParamaterisedMessage, StandardisedMessageRequest e, MessageType MessageType, StandardisedUser TargetUser = null, int Amount = -1, int NewBal = -1, string OtherString = "", string SenderUsername = null)
        {
            ParamaterisedMessage = ParamaterisedMessage.Replace("@<OtherString>", OtherString);
            ParamaterisedMessage = ParamaterisedMessage.Replace("@<CurrencyName>", BotInstance.CommandConfig["CurrencyName"].ToString());
            ParamaterisedMessage = ParamaterisedMessage.Replace("@<Amount>", Amount.ToString("N0"));
            ParamaterisedMessage = ParamaterisedMessage.Replace("@<NewBalance>", NewBal.ToString("N0"));
            ParamaterisedMessage = ParamaterisedMessage.Replace("@<CurrencyAcronym>", BotInstance.CommandConfig["CurrencyAcronym"].ToString());
            ParamaterisedMessage = ParamaterisedMessage.Replace("@<Prefix>", BotInstance.CommandConfig["Prefix"].ToString());

            foreach (Newtonsoft.Json.Linq.JToken Emote in BotInstance.CommandConfig["Emotes"])
            {
                if (MessageType == MessageType.Discord) { ParamaterisedMessage = ParamaterisedMessage.Replace("@<" + Emote["Name"].ToString() + ">", Emote["Discord"].ToString()); }
                if (MessageType == MessageType.Twitch) { ParamaterisedMessage = ParamaterisedMessage.Replace("@<" + Emote["Name"].ToString() + ">", Emote["Twitch"].ToString()); }
            }

            if (MessageType == MessageType.Twitch)
            {
                if (TargetUser != null) { ParamaterisedMessage = ParamaterisedMessage.Replace("@<TargetUser>", "@" + TargetUser.UserName); }
                if (e!=null) if (e.SenderUserName != null) { ParamaterisedMessage = ParamaterisedMessage.Replace("@<SenderUser>", "@" + e.SenderUserName); }
                else { ParamaterisedMessage = ParamaterisedMessage.Replace("@<SenderUser>", "@" + SenderUsername); }
            }
            else
            {
                if (TargetUser != null) { ParamaterisedMessage = ParamaterisedMessage.Replace("@<TargetUser>", "<@" + TargetUser.ID + ">"); }
                ParamaterisedMessage = ParamaterisedMessage.Replace("/me", "");
                if (e != null) ParamaterisedMessage = ParamaterisedMessage.Replace("@<SenderUser>", "<@" + e.SenderID + ">");
                
            }
            return ParamaterisedMessage;
        }
    }
}
