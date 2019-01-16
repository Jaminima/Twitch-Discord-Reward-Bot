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

        public Dictionary<string, string> SongRequestHistory = new Dictionary<string, string> { };
        async Task HandleThread(StandardisedMessageRequest e)
        {
            try
            {
                RewardForChatting(e);
                #region "Commands"
                if (e.SenderID != BotInstance.DiscordBot.Client.CurrentUser.Id.ToString())
                {
                    string Prefix = BotInstance.CommandConfig["Prefix"].ToString(),
                        Command = e.SegmentedBody[0].Replace(Prefix, "").ToLower();

                    if (e.MessageType == MessageType.Discord && BotInstance.CommandConfig["Discord"]["Channels"].Where(x => x.ToString() == e.ChannelID).Count() == 0) { return; }

                    if (e.SegmentedBody[0].StartsWith(Prefix) && !e.SegmentedBody[0].StartsWith(Prefix + Prefix))
                    {
                        Objects.Viewer.MergeAccounts(e, BotInstance, e.SenderID);
                        #region "Viewer"
                        #region "Notifications"
                        if (CommandEnabled(BotInstance.CommandConfig["LiveNotifications"], e) &&
                            JArrayContainsString(BotInstance.CommandConfig["LiveNotifications"]["Commands"], Command))
                        {
                            if (e.SegmentedBody.Length == 2)
                            {
                                Objects.Viewer V = Objects.Viewer.FromTwitchDiscord(e, BotInstance, e.SenderID);
                                if (e.SegmentedBody[1].ToLower() == "on")
                                {
                                    V.LiveNotifcations = true;
                                    await SendMessage(BotInstance.CommandConfig["LiveNotifications"]["Responses"]["On"].ToString(), e);
                                }
                                else if (e.SegmentedBody[1].ToLower() == "off")
                                {
                                    V.LiveNotifcations = false;
                                    await SendMessage(BotInstance.CommandConfig["LiveNotifications"]["Responses"]["Off"].ToString(), e);
                                }
                                List<KeyValuePair<string, string>> Headers = new List<KeyValuePair<string, string>> {
                                    new KeyValuePair<string, string>("ID",V.ID.ToString()),
                                    new KeyValuePair<string, string>("Notifications",V.LiveNotifcations.ToString())
                                };
                                Data.APIIntergrations.RewardCurrencyAPI.WebRequests.PostRequest("viewer", Headers, true);
                            }
                        }
                        #endregion
                        else if (CommandEnabled(BotInstance.CommandConfig["CommandSetup"]["Balance"], e) &&
                            JArrayContainsString(BotInstance.CommandConfig["CommandSetup"]["Balance"]["Commands"], Command))
                        {
                            if (LiveCheck(BotInstance.CommandConfig["CommandSetup"]["Balance"]))
                            {
                                if (e.SegmentedBody.Length == 1)
                                {
                                    Objects.Viewer B = Objects.Viewer.FromTwitchDiscord(e, BotInstance, e.SenderID);
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
                                        Objects.Viewer B = Objects.Viewer.FromTwitchDiscord(e, BotInstance, U.ID);
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
                        else if (CommandEnabled(BotInstance.CommandConfig["CommandSetup"]["WatchTime"],e)&&
                            JArrayContainsString(BotInstance.CommandConfig["CommandSetup"]["WatchTime"]["Commands"], Command))
                        {
                            if (e.SegmentedBody.Length == 1)
                            {
                                Objects.Viewer B = Objects.Viewer.FromTwitchDiscord(e, BotInstance, e.SenderID);
                                if (B != null)
                                {
                                    string Duration = AgeString(TimeSpan.FromMinutes(B.WatchTime));
                                    await SendMessage(BotInstance.CommandConfig["CommandSetup"]["WatchTime"]["Responses"]["Self"].ToString(), e, OtherString:Duration);
                                }
                                else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["APIError"].ToString(), e); }
                            }
                            else if (e.SegmentedBody.Length == 2)
                            {
                                StandardisedUser U = IDFromMessageSegment(e.SegmentedBody[1], e);
                                if (U.ID != null)
                                {
                                    Objects.Viewer B = Objects.Viewer.FromTwitchDiscord(e, BotInstance, U.ID);
                                    if (B != null)
                                    {
                                        string Duration = AgeString(TimeSpan.FromMinutes(B.WatchTime));
                                        await SendMessage(BotInstance.CommandConfig["CommandSetup"]["WatchTime"]["Responses"]["Other"].ToString(), e, U, OtherString: Duration);
                                    }
                                    else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["APIError"].ToString(), e); }
                                }
                                else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["APIError"].ToString(), e); }
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
                                        Objects.Viewer Self = Objects.Viewer.FromTwitchDiscord(e, BotInstance, e.SenderID),
                                            Other = Objects.Viewer.FromTwitchDiscord(e, BotInstance, U.ID);
                                        int ChangeBy = ValueFromMessageSegment(e.SegmentedBody[2], Self),
                                            MinPayment = int.Parse(BotInstance.CommandConfig["CommandSetup"]["Pay"]["MinimumPayment"].ToString());
                                        if (ChangeBy == -1) { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["NumberParamaterInvalid"].ToString(), e); return; }
                                        if (Self != null && Other != null)
                                        {
                                            if (ChangeBy >= 0)
                                            {
                                                if (ChangeBy >= MinPayment)
                                                {

                                                    if (Self.Balance - ChangeBy >= 0)
                                                    {
                                                        if (Objects.Viewer.AdjustBalance(Self, ChangeBy, "-"))
                                                        {
                                                            if (Objects.Viewer.AdjustBalance(Other, ChangeBy, "+"))
                                                            {
                                                                await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Pay"]["Responses"]["Paid"].ToString(), e, U, ChangeBy);
                                                            }
                                                            else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["APIError"].ToString(), e); }
                                                        }
                                                        else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["APIError"].ToString(), e); }
                                                    }
                                                    else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Pay"]["Responses"]["NotEnough"].ToString(), e); }
                                                }
                                                else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Pay"]["Responses"]["TooSmall"].ToString(), e, null, MinPayment); }
                                            }
                                            else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["NumberParamaterNegative"].ToString(), e); }
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
                                    Objects.Viewer Self = Objects.Viewer.FromTwitchDiscord(e, BotInstance, e.SenderID);
                                    int ChangeBy = ValueFromMessageSegment(e.SegmentedBody[1], Self),
                                           MinPayment = int.Parse(BotInstance.CommandConfig["CommandSetup"]["Gamble"]["MinimumPayment"].ToString()),
                                           WinChance = int.Parse(BotInstance.CommandConfig["CommandSetup"]["Gamble"]["WinChance"].ToString()),
                                           WinMultiplyer = int.Parse(BotInstance.CommandConfig["CommandSetup"]["Gamble"]["WinMultiplyer"].ToString());
                                    if (ChangeBy == -1) { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["NumberParamaterInvalid"].ToString(), e); return; }
                                    if (Self != null)
                                    {
                                        if (ChangeBy >= 0)
                                        {
                                            if (ChangeBy >= MinPayment)
                                            {

                                                if (Self.Balance - ChangeBy >= 0)
                                                {
                                                    string Operator;
                                                    if (Init.Rnd.Next(0, 100) <= WinChance) { Operator = "+"; ChangeBy *= WinMultiplyer; }
                                                    else { Operator = "-"; }
                                                    if (Objects.Viewer.AdjustBalance(Self, ChangeBy, Operator))
                                                    {
                                                        if (Operator == "+") { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Gamble"]["Responses"]["Win"].ToString(), e, null, ChangeBy); }
                                                        else if (Operator == "-") { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Gamble"]["Responses"]["Lose"].ToString(), e, null, ChangeBy); }
                                                    }
                                                    else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["APIError"].ToString(), e); }
                                                }
                                                else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Pay"]["Responses"]["NotEnough"].ToString(), e); }
                                            }
                                            else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Pay"]["Responses"]["TooSmall"].ToString(), e, null, MinPayment); }
                                        }
                                        else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["NumberParamaterNegative"].ToString(), e); }
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
                                    Objects.Viewer Self = Objects.Viewer.FromTwitchDiscord(e, BotInstance, e.SenderID);
                                    int ChangeBy = ValueFromMessageSegment(e.SegmentedBody[1], Self),
                                        MinPayment = int.Parse(BotInstance.CommandConfig["CommandSetup"]["Slots"]["MinimumPayment"].ToString()),
                                        WinChance = int.Parse(BotInstance.CommandConfig["CommandSetup"]["Slots"]["WinChance"].ToString()),
                                        WinMultiplyer = int.Parse(BotInstance.CommandConfig["CommandSetup"]["Slots"]["WinMultiplyer"].ToString());
                                    if (ChangeBy == -1) { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["NumberParamaterInvalid"].ToString(), e); return; }
                                    if (Self != null)
                                    {
                                        if (ChangeBy >= 0)
                                        {
                                            if (ChangeBy >= MinPayment)
                                            {

                                                if (Self.Balance - ChangeBy >= 0)
                                                {
                                                    string Operator;
                                                    if (Init.Rnd.Next(0, 100) <= WinChance) { Operator = "+"; ChangeBy *= WinMultiplyer; }
                                                    else { Operator = "-"; }
                                                    if (Objects.Viewer.AdjustBalance(Self, ChangeBy, Operator))
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
                                            else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Pay"]["Responses"]["TooSmall"].ToString(), e, null, MinPayment); }
                                        }
                                        else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["NumberParamaterNegative"].ToString(), e); }
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
                                    Objects.Viewer Self = Objects.Viewer.FromTwitchDiscord(e, BotInstance, e.SenderID);
                                    int ViewerCost = int.Parse(BotInstance.CommandConfig["CommandSetup"]["Fish"]["Cost"]["Viewer"].ToString()),
                                        SubscriberCost = int.Parse(BotInstance.CommandConfig["CommandSetup"]["Fish"]["Cost"]["Subscriber"].ToString());
                                    int Cost = ViewerCost;
                                    if (Self != null)
                                    {
                                        if (IsSubscriber(e)) { Cost = SubscriberCost; }
                                        if (Self.Balance - Cost >= 0)
                                        {
                                            if (BotInstance.TimeEvents.Fishermen.Where(x => x.Value.e.SenderID == e.SenderID).Count() == 0)
                                            {
                                                if (Objects.Viewer.AdjustBalance(Self, Cost, "-"))
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
                                        Objects.Viewer Self = Objects.Viewer.FromTwitchDiscord(e, BotInstance, e.SenderID),
                                            TargetBank = Objects.Viewer.FromTwitchDiscord(e, BotInstance, Target.ID);
                                        if (Self != null && TargetBank != null)
                                        {
                                            int ChangeBy = ValueFromMessageSegment(e.SegmentedBody[2], Self),
                                                TargetChangeBy = ValueFromMessageSegment(e.SegmentedBody[2], TargetBank);
                                            if (ChangeBy != -1 && TargetChangeBy != -1)
                                            {
                                                if (TargetChangeBy < ChangeBy) { ChangeBy = TargetChangeBy; }
                                                if (ChangeBy >= 0)
                                                {
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
                                                else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["NumberParamaterNegative"].ToString(), e); }
                                            }
                                            else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["NumberParamaterInvalid"].ToString(), e); }
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
                                Objects.Viewer Acceptor = Objects.Viewer.FromTwitchDiscord(e, BotInstance, TDuel.Value.Acceptor.ID),
                                    Creator = Objects.Viewer.FromTwitchDiscord(e, BotInstance, TDuel.Value.Creator.ID);
                                if (Acceptor != null && Creator != null)
                                {
                                    if (TDuel.Value.ChangeBy <= Acceptor.Balance)
                                    {
                                        if (TDuel.Value.ChangeBy <= Creator.Balance)
                                        {
                                            int Winner = Init.Rnd.Next(0, 2);
                                            if (Winner == 0)
                                            {
                                                Objects.Viewer.AdjustBalance(Acceptor, TDuel.Value.ChangeBy, "+");
                                                Objects.Viewer.AdjustBalance(Creator, TDuel.Value.ChangeBy, "-");
                                                await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Duel"]["Accepting"]["Responses"]["Win"].ToString(), e, TDuel.Value.Creator, TDuel.Value.ChangeBy);
                                            }
                                            if (Winner == 1)
                                            {
                                                Objects.Viewer.AdjustBalance(Acceptor, TDuel.Value.ChangeBy, "-");
                                                Objects.Viewer.AdjustBalance(Creator, TDuel.Value.ChangeBy, "+");
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
                        else if (CommandEnabled(BotInstance.CommandConfig["Raffle"], e) &&
                            JArrayContainsString(BotInstance.CommandConfig["Raffle"]["Joining"]["Commands"], Command))
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
                        #endregion
                        #region "NightBot"
                        else if (CommandEnabled(BotInstance.CommandConfig["CommandSetup"]["NightBot"], e) &&
                            JArrayContainsString(BotInstance.CommandConfig["CommandSetup"]["NightBot"]["Playlist"]["Commands"], Command))
                        {
                            await SendMessage(BotInstance.CommandConfig["CommandSetup"]["NightBot"]["Playlist"]["Response"].ToString(), e);
                        }
                        else if (CommandEnabled(BotInstance.CommandConfig["CommandSetup"]["NightBot"], e) &&
                           JArrayContainsString(BotInstance.CommandConfig["CommandSetup"]["NightBot"]["Queue"]["Commands"], Command))
                        {
                            await SendMessage(BotInstance.CommandConfig["CommandSetup"]["NightBot"]["Queue"]["Response"].ToString(), e);
                        }
                        else if (CommandEnabled(BotInstance.CommandConfig["CommandSetup"]["NightBot"], e) &&
                            JArrayContainsString(BotInstance.CommandConfig["CommandSetup"]["NightBot"]["Request"]["Commands"], Command))
                        {
                            if (e.SegmentedBody.Length >= 2)
                            {
                                string Request = e.MessageBody.Replace(e.SegmentedBody[0] + " ", "");
                                int Cost = int.Parse(BotInstance.CommandConfig["CommandSetup"]["NightBot"]["Request"]["Cost"]["Viewer"].ToString()),
                                    SubscriberCost = int.Parse(BotInstance.CommandConfig["CommandSetup"]["NightBot"]["Request"]["Cost"]["Subscriber"].ToString());
                                if (IsSubscriber(e)) { Cost = SubscriberCost; }
                                Objects.Viewer B = Objects.Viewer.FromTwitchDiscord(e, BotInstance, e.SenderID);
                                if (B.Balance >= Cost)
                                {
                                    Newtonsoft.Json.Linq.JToken JData = Data.APIIntergrations.Nightbot.RequestSong(BotInstance, Request);
                                    if (JData["status"].ToString() == "200")
                                    {
                                        if (!SongRequestHistory.ContainsKey(e.SenderID)) { SongRequestHistory.Add(e.SenderID, JData["item"]["_id"].ToString()); }
                                        else { SongRequestHistory[e.SenderID] = JData["item"]["_id"].ToString(); }
                                        Objects.Viewer.AdjustBalance(B, Cost, "-");
                                        string MessageContent = JData["item"]["track"]["title"] + " by " + JData["item"]["track"]["artist"] + " -- " + JData["item"]["track"]["url"];
                                        await SendMessage(BotInstance.CommandConfig["CommandSetup"]["NightBot"]["Request"]["Responses"]["Requested"].ToString(), e, OtherString: MessageContent);
                                    }
                                    else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["NightBot"]["Responses"]["APIError"].ToString(), e, OtherString: JData["message"].ToString()); }
                                }
                                else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["NightBot"]["Request"]["Responses"]["NotEnough"].ToString(), e); }
                            }
                            else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["ParamaterCount"].ToString(), e); }
                        }
                        else if (CommandEnabled(BotInstance.CommandConfig["CommandSetup"]["NightBot"], e) &&
                            JArrayContainsString(BotInstance.CommandConfig["CommandSetup"]["NightBot"]["Cancel"]["Commands"], Command))
                        {
                            if (SongRequestHistory.ContainsKey(e.SenderID))
                            {
                                Newtonsoft.Json.Linq.JToken JData = Data.APIIntergrations.Nightbot.GetQueue(BotInstance);
                                if (JData["status"].ToString() == "200")
                                {
                                    if (JData["queue"].Where(x => x["_id"].ToString() == SongRequestHistory[e.SenderID]).Count() != 0)
                                    {
                                        JData = Data.APIIntergrations.Nightbot.RemoveID(BotInstance, SongRequestHistory[e.SenderID]);
                                        if (JData["status"].ToString() == "200")
                                        {
                                            await SendMessage(BotInstance.CommandConfig["CommandSetup"]["NightBot"]["Cancel"]["Responses"]["CanceledSong"].ToString(), e);
                                        }
                                        else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["NightBot"]["Responses"]["APIError"].ToString(), e, OtherString: JData["message"].ToString()); }
                                    }
                                    else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["NightBot"]["Cancel"]["Responses"]["SongDoesntExist"].ToString(), e); }
                                }
                                else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["NightBot"]["Responses"]["APIError"].ToString(), e, OtherString: JData["message"].ToString()); }
                            }
                            else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["NightBot"]["Cancel"]["Responses"]["NoSong"].ToString(), e); }
                        }
                        else if (CommandEnabled(BotInstance.CommandConfig["CommandSetup"]["NightBot"], e) &&
                            JArrayContainsString(BotInstance.CommandConfig["CommandSetup"]["NightBot"]["Current"]["Commands"], Command))
                        {
                            Newtonsoft.Json.Linq.JToken JData = Data.APIIntergrations.Nightbot.GetQueue(BotInstance);
                            if (JData["status"].ToString() == "200")
                            {
                                if (JData["_currentSong"].HasValues)
                                {
                                    string MessageContent = JData["_currentSong"]["track"]["title"] + " by " + JData["_currentSong"]["track"]["artist"] + " -- " + JData["_currentSong"]["track"]["url"];
                                    await SendMessage(BotInstance.CommandConfig["CommandSetup"]["NightBot"]["Current"]["Responses"]["CurrentlyPlaying"].ToString(), e, OtherString: MessageContent);
                                }
                                else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["NightBot"]["Current"]["Responses"]["NotPlaying"].ToString(), e); }
                            }
                            else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["NightBot"]["Responses"]["APIError"].ToString(), e, OtherString: JData["message"].ToString()); }
                        }
                        #endregion
                        #region "Moderator"
                        else if (CommandEnabled(BotInstance.CommandConfig["CommandSetup"]["Moderator"]["SetTitle"], e) &&
                            JArrayContainsString(BotInstance.CommandConfig["CommandSetup"]["Moderator"]["SetTitle"]["Commands"], Command))
                        {
                            if (IsModerator(e))
                            {
                                string Title = e.MessageBody.Replace(e.SegmentedBody[0] + " ", "");
                                Data.APIIntergrations.Twitch.UpdateChannelTitle(BotInstance, Title);
                                await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Moderator"]["SetTitle"]["Responses"]["SetTitle"].ToString(), e, null, -1, -1, Title);
                            }
                            else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Moderator"]["Responses"]["NotMod"].ToString(), e); }
                        }
                        else if (CommandEnabled(BotInstance.CommandConfig["CommandSetup"]["Moderator"]["SetGame"], e) &&
                           JArrayContainsString(BotInstance.CommandConfig["CommandSetup"]["Moderator"]["SetGame"]["Commands"], Command))
                        {
                            if (IsModerator(e))
                            {
                                string Game = e.MessageBody.Replace(e.SegmentedBody[0] + " ", "");
                                Data.APIIntergrations.Twitch.UpdateChannelGame(BotInstance, Game);
                                await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Moderator"]["SetGame"]["Responses"]["SetGame"].ToString(), e, null, -1, -1, Game);
                            }
                            else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Moderator"]["Responses"]["NotMod"].ToString(), e); }
                        }
                        else if (CommandEnabled(BotInstance.CommandConfig["CommandSetup"]["Moderator"]["GiveCoin"], e) &&
                            JArrayContainsString(BotInstance.CommandConfig["CommandSetup"]["Moderator"]["GiveCoin"]["Commands"], Command))
                        {
                            if (IsModerator(e))
                            {
                                if (e.SegmentedBody.Length == 3)
                                {
                                    Objects.Viewer B = Objects.Viewer.FromTwitchDiscord(e, BotInstance, e.SenderID);
                                    int ChangeBy = ValueFromMessageSegment(e.SegmentedBody[2], B);
                                    if (ChangeBy != -1)
                                    {
                                        if (ChangeBy >= 0)
                                        {
                                            StandardisedUser S = null;
                                            if (e.MessageType == MessageType.Twitch) { S = StandardisedUser.FromTwitchUsername(e.SegmentedBody[1], BotInstance); }
                                            if (e.MessageType == MessageType.Discord) { S = StandardisedUser.FromDiscordMention(e.SegmentedBody[1], BotInstance); }
                                            B = Objects.Viewer.FromTwitchDiscord(e.MessageType, BotInstance, S.ID);
                                            if (Objects.Viewer.AdjustBalance(B, ChangeBy, "+"))
                                            {
                                                await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Moderator"]["GiveCoin"]["Responses"]["Gave"].ToString(), e, S, ChangeBy);
                                            }
                                            else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["APIError"].ToString(), e); }
                                        }
                                        else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["NumberParamaterNegative"].ToString(), e); }
                                    }
                                    else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["NumberParamaterInvalid"].ToString(), e); }
                                }
                                else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["ParamaterCount"].ToString(), e); }
                            }
                            else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Moderator"]["Responses"]["NotMod"].ToString(), e); }
                        }
                        else if (CommandEnabled(BotInstance.CommandConfig["CommandSetup"]["NightBot"], e)&&
                            JArrayContainsString(BotInstance.CommandConfig["CommandSetup"]["NightBot"]["Moderator"]["Pause"]["Commands"],Command))
                        {
                            if (IsModerator(e))
                            {
                                Newtonsoft.Json.Linq.JToken JData = Data.APIIntergrations.Nightbot.PauseSong(BotInstance);
                                if (JData["status"].ToString() == "200")
                                {
                                    await SendMessage(BotInstance.CommandConfig["CommandSetup"]["NightBot"]["Moderator"]["Pause"]["Response"].ToString(), e);
                                }
                                else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["NightBot"]["Responses"]["APIError"].ToString(), e, OtherString: JData["message"].ToString()); }

                            }
                        }
                        else if (CommandEnabled(BotInstance.CommandConfig["CommandSetup"]["NightBot"], e) &&
                           JArrayContainsString(BotInstance.CommandConfig["CommandSetup"]["NightBot"]["Moderator"]["Play"]["Commands"], Command))
                        {
                            if (IsModerator(e))
                            {
                                Newtonsoft.Json.Linq.JToken JData = Data.APIIntergrations.Nightbot.PlaySong(BotInstance);
                                if (JData["status"].ToString() == "200")
                                {
                                    await SendMessage(BotInstance.CommandConfig["CommandSetup"]["NightBot"]["Moderator"]["Play"]["Response"].ToString(), e);
                                }
                                else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["NightBot"]["Responses"]["APIError"].ToString(), e, OtherString: JData["message"].ToString()); }

                            }
                        }
                        else if (CommandEnabled(BotInstance.CommandConfig["CommandSetup"]["NightBot"], e) &&
                           JArrayContainsString(BotInstance.CommandConfig["CommandSetup"]["NightBot"]["Moderator"]["Skip"]["Commands"], Command))
                        {
                            if (IsModerator(e))
                            {
                                Newtonsoft.Json.Linq.JToken JData = Data.APIIntergrations.Nightbot.SkipSong(BotInstance);
                                if (JData["status"].ToString() == "200")
                                {
                                    await SendMessage(BotInstance.CommandConfig["CommandSetup"]["NightBot"]["Moderator"]["Skip"]["Response"].ToString(), e);
                                }
                                else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["NightBot"]["Responses"]["APIError"].ToString(), e, OtherString: JData["message"].ToString()); }
                            }
                        }
                        else if (CommandEnabled(BotInstance.CommandConfig["CommandSetup"]["NightBot"], e) &&
                           JArrayContainsString(BotInstance.CommandConfig["CommandSetup"]["NightBot"]["Moderator"]["Remove"]["Commands"], Command))
                        {
                            if (IsModerator(e))
                            {
                                if (e.SegmentedBody.Length == 2)
                                {
                                    try { int.Parse(e.SegmentedBody[1]); } catch { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["NumberParamaterInvalid"].ToString(), e); return; }
                                    Newtonsoft.Json.Linq.JToken JData = Data.APIIntergrations.Nightbot.RemoveItem(BotInstance, int.Parse(e.SegmentedBody[1]));
                                    if (JData["status"].ToString() == "200")
                                    {
                                        string MessageContent = JData["item"]["track"]["title"] + " by " + JData["item"]["track"]["artist"] + " -- " + JData["item"]["track"]["url"];
                                        await SendMessage(BotInstance.CommandConfig["CommandSetup"]["NightBot"]["Moderator"]["Remove"]["Response"].ToString(), e,OtherString:MessageContent);
                                    }
                                    else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["NightBot"]["Responses"]["APIError"].ToString(), e, OtherString: JData["message"].ToString()); }

                                }
                            }
                        }
                        #endregion
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
                #endregion
            }
            catch (Exception E) { Console.WriteLine(E); }
        }

        public void RewardForChatting(StandardisedMessageRequest e)
        {
            if (BotInstance.TimeEvents != null)
            {
                IEnumerable<Viewer> Vs = BotInstance.TimeEvents.ViewerRewardTracking.Where(x => x.User.ID == e.SenderID);
                if (Vs.Count() != 0)
                {
                    Viewer V = Vs.First();
                    int TwitchDelay = int.Parse(BotInstance.CommandConfig["AutoRewards"]["Chatting"]["Twitch"]["Interval"].ToString()),
                        TwitchReward = int.Parse(BotInstance.CommandConfig["AutoRewards"]["Chatting"]["Twitch"]["Reward"].ToString()),
                        DiscordDelay = int.Parse(BotInstance.CommandConfig["AutoRewards"]["Chatting"]["Discord"]["Interval"].ToString()),
                        DiscordReward = int.Parse(BotInstance.CommandConfig["AutoRewards"]["Chatting"]["Discord"]["Reward"].ToString());
                    if (e.MessageType == MessageType.Twitch)
                    {
                        if (((TimeSpan)(DateTime.Now - V.LastTwitchMessage)).TotalSeconds >= TwitchDelay)
                        {
                            V.LastTwitchMessage = DateTime.Now;
                            Objects.Viewer B = Objects.Viewer.FromTwitchDiscord(e.MessageType, BotInstance, e.SenderID);
                            Objects.Viewer.AdjustBalance(B, TwitchReward, "+");
                        }
                    }
                    else if (e.MessageType == MessageType.Discord)
                    {
                        if (((TimeSpan)(DateTime.Now - V.LastTwitchMessage)).TotalSeconds >= DiscordDelay)
                        {
                            V.LastTwitchMessage = DateTime.Now;
                            Objects.Viewer B = Objects.Viewer.FromTwitchDiscord(e.MessageType, BotInstance, e.SenderID);
                            Objects.Viewer.AdjustBalance(B, DiscordReward, "+");
                        }
                    }
                }
                else {
                    Viewer V = new Viewer();
                    StandardisedUser U = new StandardisedUser();
                    U.ID = e.SenderID; U.UserName = e.SenderUserName;
                    V.User = U;
                    BotInstance.TimeEvents.ViewerRewardTracking.Add(V);
                    RewardForChatting(e);
                }
            }
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

        public int ValueFromMessageSegment(string MessageSegment,Objects.Viewer Bank)
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

        public string AgeString(TimeSpan Span)
        {
            string Age = "";
            int Years = (int)Math.Floor((decimal)Span.Days / 365);
            int Months = (int)Math.Floor((decimal)(Span.Days - (Years * 365)) / 30);
            int Days = Span.Days - ((Years * 365) + (Months * 30));

            if (Years != 0) { if (Years == 1) { Age += Years + " Year"; } else { Age += Years + " Years"; } }

            if (Months != 0 && Days == 0 && Span.Hours == 0&&Span.Minutes== 0 && Age != "") { Age += " and "; }
            if (Months != 0) { if (Age != "") { Age += " "; } if (Months == 1) { Age += Months + " Month"; } else { Age += Months + " Months"; } }

            if (Days != 0 && Span.Hours == 0&&Span.Minutes== 0 && Age != "") { Age += " and "; }
            if (Days != 0) { if (Age != "") { Age += " "; } if (Days == 1) { Age += Days + " Day"; } else { Age += Days + " Days"; } }

            if (Span.Hours != 0 && Span.Minutes == 0 && Age != "") { Age += " and "; }
            if (Span.Hours != 0) { if (Age != "") { Age += " "; } if (Span.Hours == 1) { Age += Span.Hours + " Hour"; } else { Age += Span.Hours + " Hours"; } }


            if (Span.Minutes != 0 && Age!="") { Age += " and "; }
            if (Span.Minutes != 0) { if (Age != "") { Age += " "; } if (Span.Minutes == 1) { Age += Span.Minutes + " Minute"; } else { Age += Span.Minutes + " Minutes"; } }

            return Age;
        }

        public bool IsModerator(StandardisedMessageRequest e)
        {
            if (e.MessageType == MessageType.Twitch) { return e.TwitchRaw.ChatMessage.IsModerator || e.TwitchRaw.ChatMessage.IsBroadcaster; }
            else if (e.MessageType == MessageType.Discord)
            {
                return ((SocketGuildUser)e.DiscordRaw.Author).Roles.Where(x => x.Id.ToString() == BotInstance.CommandConfig["Discord"]["ModeratorRoleID"].ToString()).Count() != 0;
            }
            return false;
        }

        public bool IsSubscriber(StandardisedMessageRequest e)
        {
            if (e.MessageType == MessageType.Twitch)
            { if (e.TwitchRaw.ChatMessage.IsSubscriber) { return true; } }
            if (e.MessageType == MessageType.Discord)
            { if (((SocketGuildUser)e.DiscordRaw.Author).Roles.Where(x => x.Id.ToString() == BotInstance.CommandConfig["Discord"]["SubscriberRoleID"].ToString()).Count() != 0) { return true; } }
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
            ParamaterisedMessage = ParamaterisedMessage.Replace("@<ChannelName>", BotInstance.CommandConfig["ChannelName"].ToString());
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
