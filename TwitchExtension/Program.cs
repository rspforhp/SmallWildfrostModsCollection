using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Deadpan.Enums.Engine.Components.Modding;
using HttpListenerExample;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using WebSocketSharp;
using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;

namespace TwitchExtension
{
    public class TwitchExtensionMod : WildfrostMod
    {
        [ConfigItem("none", forceTitle = "login")]
        public string login;

        public TwitchExtensionMod(string modDirectory) : base(modDirectory)
        {
            Instance = this;
        }

        public HttpClient Client;
        public static TwitchExtensionMod Instance;

        protected override void Load()
        {
            base.Load();

            System.Console.WriteLine($"Config twitch mod login: " + login);
            if (string.IsNullOrEmpty(login) || login == "none")
            {
                System.Console.Error.WriteLine($"Enter login in config file for twitch mod.");
                return;
            }

            Client = new HttpClient();
            var prc = System.Diagnostics.Process.Start(
                @"https://id.twitch.tv/oauth2/authorize?response_type=token&client_id=iymxstcdumf1galjddbfwp8hzh01by&scope=chat:read+user:write:chat+user:read:chat+chat:edit+channel:read:redemptions+channel:manage:redemptions+channel:manage:polls+channel:manage:predictions+channel:read:polls+channel:read:predictions+moderator:manage:announcements+moderator:manage:chat_messages+moderator:read:chatters+user:read:chat&redirect_uri=http://localhost:3000");
            Task.Run(async delegate { HttpServer.Main(new string[0]); });
            while (HttpServer.LastQuery == null || HttpServer.LastQuery?.Count < 1)
            {
                ;
            }

            var query = HttpServer.LastQuery;
            var access_token = query["access_token"];
            var scope = query["scope"];
            var token_type = query["token_type"];
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access_token);
            Client.DefaultRequestHeaders.Add("Client-Id", $"iymxstcdumf1galjddbfwp8hzh01by");
            Client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", $"application/json");
            var getUserRequest = Client.GetAsync($"https://api.twitch.tv/helix/users?login={login}").Result;
            StremerData = Deserialize<UsersPayload>(getUserRequest.Content.ReadAsStringAsync().Result).data[0];
            var getPolls = Client.GetAsync($"https://api.twitch.tv/helix/polls?broadcaster_id={StremerData.id}").Result;
            var result = getPolls.StatusCode;
            if (result == (HttpStatusCode.Unauthorized))
            {
                WriteLine($"ERROR: Login in the config file isn't yours!");
                StremerData = null;
            }
            else
            {
                WriteLine($"Logged in for: {StremerData.id} {StremerData.display_name} !");
            }


            Task.Run(async delegate
            {
                try
                {
                    TwitchEventSub = new WebSocket("wss://eventsub.wss.twitch.tv/ws");
                    TwitchEventSub.SslConfiguration.EnabledSslProtocols =
                        System.Security.Authentication.SslProtocols.Tls12;
                    TwitchEventSub.OnMessage += delegate(object sender, MessageEventArgs e)
                    {
                     
                        var minimalMsg =
                            Deserialize<TwitchMessage<TwitchMessageMetadata, TwitchMessagePayload>>(e.Data);
                        if (minimalMsg != null)
                        {
                            switch (minimalMsg.metadata.message_type)
                            {
                                case TwitchMessageMetadata.MessageType.@null:
                                    break;
                                case TwitchMessageMetadata.MessageType.session_welcome:
                                {
                                    var welcome = Deserialize<WelcomeMessage>(e.Data);
                                    EventSubSession = welcome.payload.session;
                                }
                                    break;
                                case TwitchMessageMetadata.MessageType.session_keepalive:
                                {
                                    var keep = Deserialize<KeepAliveMessage>(e.Data);
                                    if (LastKeepAlive == null)
                                    {
                                        LastKeepAlive = keep;
                                        AddSubscribers();
                                    }
                                    LastKeepAlive = CurrentKeepAlive;
                                    CurrentKeepAlive = keep;
                                }
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
 
                        }
                     
                        System.Console.WriteLine("wss says: " + e.Data);
                    };

                    TwitchEventSub.OnOpen += delegate(object sender, EventArgs args)
                    {
                        System.Console.WriteLine("eventsub open! " + args);
                    };
                    TwitchEventSub.OnError += delegate(object sender, ErrorEventArgs args)
                    {
                        System.Console.WriteLine("eventsub error! " + args);
                    };

                    TwitchEventSub.ConnectAsync();
                    TwitchEventSub.Ping() ;

                    SendMessage("Wildfrost Twitch mod turned on!:3");
                    bool run = true;
                    while (run)
                    {
                        break;
                        if (EventSubSession != null)
                        {
                            var timeout = EventSubSession.keepalive_timeout_seconds;

                            if (LastKeepAlive != null && CurrentKeepAlive != null &&
                                LastKeepAlive.metadata.mmessage_timestamp !=
                                CurrentKeepAlive.metadata.mmessage_timestamp)
                            {
                                System.Console.WriteLine($"Last {LastKeepAlive.metadata.mmessage_timestamp}");
                                System.Console.WriteLine($"Cur {CurrentKeepAlive.metadata.mmessage_timestamp}");
                                if (LastKeepAlive.metadata.mmessage_timestamp.AddSeconds(10) >
                                    CurrentKeepAlive.metadata.mmessage_timestamp)
                                {
                                    System.Console.WriteLine($"Didn't receive keepalive msg in time, shutting down.");
                                    run = false;
                                    TwitchEventSub.CloseAsync();
                                    TwitchEventSub = null;
                                }
                            }
                        }

                        await Task.Delay(1001);
                    }
                }
                catch (Exception e)
                {
                    System.Console.WriteLine(e);
                    throw;
                }
            });
        }

        /*
         * type	String	Yes	The subscription type name: channel.chat.message.
version	String	Yes	The subscription type version: 1.
condition	 condition 	Yes	Subscription-specific parameters.
transport	 transport 	Yes	Transport-specific parameters.
         */
        public class ChannelChatMessage
        {
            public string broadcaster_user_id { get; set; }
            public string user_id { get; set; }

        }
        private void AddSubscribers()
        {
            SendEventSubSubscription(SubscriptionTypes.channel_chat_message,new ChannelChatMessage(){broadcaster_user_id = StremerData.id, user_id = StremerData.id} );
        }
        public KeepAliveMessage CurrentKeepAlive;
        public KeepAliveMessage LastKeepAlive;

        public Session EventSubSession;

        public class Session
        {
            public string id { get; set; }

            public enum Status
            {
                @null,
                connected,
            }
            [Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
            public Status status { get; set; }
            public int keepalive_timeout_seconds { get; set; }
            public string reconnect_url { get; set; }
            public string connected_at { get; set; }
        }

        public class KeepAliveMessage : TwitchMessage<TwitchMessageMetadata, TwitchMessagePayload>
        {
        }

        public class WelcomeMessage : TwitchMessage<TwitchMessageMetadata, WelcomeMessage.Payload>
        {
            public class Payload : TwitchMessagePayload
            {
                public Session session { get; set; }
            }
        }

        public class TwitchMessageMetadata
        {
            public string message_id { get; set; }

            public enum MessageType
            {
                @null,
                session_welcome,
                session_keepalive
            }

            [Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
            public MessageType message_type { get; set; }
            public string message_timestamp { get; set; }

            public string message_timestampStr =>
                message_timestamp.Substring(0, message_timestamp.LastIndexOf(".") + 8) + message_timestamp.Last();

            public DateTime mmessage_timestamp =>
                Rfc3339DateTime.Parse(message_timestampStr);
        }

        public class TwitchMessagePayload
        {
        }

        public class TwitchMessage<T, T2> where T : TwitchMessageMetadata where T2 : TwitchMessagePayload
        {
            public T metadata { get; set; }
            public T2 payload { get; set; }
        }

        public WebSocket TwitchEventSub;

        [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
        sealed class RequiresTwitchPartnerAttribute : Attribute
        {
            // See the attribute guidelines at 
            //  http://go.microsoft.com/fwlink/?LinkId=85236
            public RequiresTwitchPartnerAttribute()
            {
                // TODO: Implement code here
                throw new NotImplementedException();
            }
        }


        [RequiresTwitchPartner]
        public PollsPayload GetPolls()
        {
            var req = Client.GetAsync($"https://api.twitch.tv/helix/polls?broadcaster_id={StremerData.id}").Result;
            return Deserialize<PollsPayload>(req.Content.ReadAsStringAsync().Result);
        }

        public T Deserialize<T>(string json)
        {
            return JsonSerializer.CreateDefault().Deserialize<T>(new JsonTextReader(new StringReader(json)));
        }

        public string Serialize<T>(T json)
        {
            var sb = new StringBuilder();
            JsonSerializer.CreateDefault().Serialize(new StringWriter(sb), json);
            return sb.ToString();
        }
      public static class  SubscriptionTypes
            {
                public const string channel_update="channel.update";
                public const string channel_follow="channel.follow";
                public const string channel_ad_break_begin="channel.ad_break.begin";
                public const string channel_chat_clear="channel.chat.clear";
                public const string channel_chat_clear_user_messages="channel.chat.clear_user_messages";
                public const string channel_chat_message="channel.chat.message";
                public const string channel_chat_message_delete="channel.chat.message_delete";
                public const string channel_chat_notification="channel.chat.notification";
                public const string channel_chat_settings_update="channel.chat_settings.update";
                public const string channel_subscribe="channel.subscribe";
                public const string channel_subscription_end="channel.subscription.end";
                public const string channel_subscription_gift="channel.subscription.gift";
                public const string channel_subscription_message="channel.subscription.message";
                public const string channel_cheer="channel.cheer";
                public const string channel_raid="channel.raid";
                public const string channel_ban="channel.ban";
                public const string channel_unban="channel.unban";
                public const string channel_moderator_add="channel.moderator.add";
                public const string channel_moderator_remove="channel.moderator.remove";
                public const string channel_guest_star_session_begin="channel.guest_star_session.begin";
                public const string channel_guest_star_session_end="channel.guest_star_session.end";
                public const string channel_guest_star_guest_update="channel.guest_star_guest.update";
                public const string channel_guest_star_settings_update="channel.guest_star_settings.update";
                public const string channel_channel_points_custom_reward_add="channel.channel_points_custom_reward.add";
                public const string channel_channel_points_custom_reward_update="channel.channel_points_custom_reward.update";
                public const string channel_channel_points_custom_reward_remove="channel.channel_points_custom_reward.remove";
                public const string channel_channel_points_custom_reward_redemption_add="channel.channel_points_custom_reward_redemption.add";
                public const string channel_channel_points_custom_reward_redemption_update="channel.channel_points_custom_reward_redemption.update";
                public const string channel_poll_begin="channel.poll.begin";
                public const string channel_poll_progress="channel.poll.progress";
                public const string channel_poll_end= "channel.poll.end";
                public const string channel_prediction_begin= "channel.prediction.begin";
                public const string channel_prediction_progress= "channel.prediction.progress";
                public const string channel_prediction_lock= "channel.prediction.lock";
                public const string channel_prediction_end= "channel.prediction.end";
                public const string channel_charity_campaign_donate= "channel.charity_campaign.donate";
                public const string channel_charity_campaign_progress= "channel.charity_campaign.progress";
                public const string channel_charity_campaign_stop= "channel.charity_campaign.stop";
                public const string conduit_shard_disabled= "conduit.shard.disabled";
                public const string drop_entitlement_grant= "drop.entitlement.grant";
                public const string extension_bits_transaction_create= "extension.bits_transaction.create";
                public const string channel_goal_begin= "channel.goal.begin";
                public const string channel_goal_progress= "channel.goal.progress";
                public const string channel_goal_end= "channel.goal.end";
                public const string channel_hype_train_begin= "channel.hype_train.begin";
                public const string channel_hype_train_progress= "channel.hype_train.progress";
                public const string channel_hype_train_end= "channel.hype_train.end";
                public const string channel_shield_mode_begin= "channel.shield_mode.begin";
                public const string channel_shield_mode_end= "channel.shield_mode.end";
                public const string channel_shoutout_create= "channel.shoutout.create";
                public const string channel_shoutout_receive= "channel.shoutout.receive";
                public const string stream_online= "stream.online";
                public const string stream_offline= "stream.offline";
                public const string user_authorization_grant= "user.authorization.grant";
                public const string user_authorization_revoke= "user.authorization.revoke";
                public const string user_update= "user.update";
                private static Dictionary<string, string> Versions = new Dictionary<string, string>()
                {
                    {channel_update, "2"},
                    {channel_follow, "2"},
                    {channel_chat_settings_update, "beta"},
                    {channel_guest_star_guest_update, "beta"},
                    {channel_guest_star_session_begin, "beta"},
                    {channel_guest_star_session_end, "beta"},
                    {channel_guest_star_settings_update, "beta"},
                };

                public static string GetVersion(string key)
                {
                    return Versions.TryGetValue(key, out var version1) ? version1 : "1";
                }
            }
          
        public class SendEventSubSubscriptionPayload<T>
        {
        public string type { get; set; }
            public string version { get; set; }
            public T condition { get; set; }
            public Transport transport { get; set; }
            public string session_id { get; set; }
            public string conduit_id { get; set; }
        }
        public class Transport
        {
            public enum Method
            {
                @null,
                webhook,
                websocket,
                conduit
            }
            [Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
            public Method method { get; set; }
            public string callback{ get; set; }
            public string secret{ get; set; }
            public string session_id{ get; set; }
            public string connected_at{ get; set; }
            public string disconnected_at{ get; set; }
        }
        public void SendEventSubSubscription<T>(string subType,T condition)
        {
            var version = SubscriptionTypes.GetVersion(subType);
            var p = new SendEventSubSubscriptionPayload<T>()
                { type=subType,version = version, condition =condition, transport = new Transport(){method = Transport.Method.websocket,session_id = EventSubSession.id}};
            var json = Serialize(p);
            var c = new StringContent(json);
            c.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var req = Client.PostAsync($"https://api.twitch.tv/helix/eventsub/subscriptions", c).Result;
            System.Console.WriteLine(req.Content.ReadAsStringAsync().Result);
            System.Console.WriteLine($"what was sent: "+json);
            if (req.StatusCode == HttpStatusCode.Accepted)
            {
                System.Console.WriteLine($"Subscribed to "+subType +" succesfully!");
            }
            else                 System.Console.WriteLine($"Cant do:Subscribed to "+subType);

            //returns stuff but wtv;
        }
        
        
        
        public void SendMessage(string msg)
        {
            var p = new SendMessagePayload()
                { broadcaster_id = StremerData.id, sender_id = StremerData.id, message = msg };
            var c = new StringContent(Serialize(p));
            c.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var req = Client.PostAsync($"https://api.twitch.tv/helix/chat/messages", c).Result;
            WriteLine(req.Content.ReadAsStringAsync().Result);
            //returns stuff but wtv;
        }
        public class SendMessagePayload
        {
            public string broadcaster_id { get; set; }
            public string sender_id { get; set; }
            public string message { get; set; }
            public string reply_parent_message_id { get; set; }
        }
        public class TwitchPollData
        {
            public string id { get; set; }
            public string broadcaster_id { get; set; }
            public string broadcaster_name { get; set; }
            public string broadcaster_login { get; set; }
            public string title { get; set; }

            public class Choice
            {
                public string id { get; set; }
                public string title { get; set; }
                public int votes { get; set; }
                public int channel_points_votes { get; set; }
                public int bits_votes { get; set; }
            }

            public Choice[] choices { get; set; }
            public bool bits_voting_enabled { get; set; }
            public int bits_per_vote { get; set; }
            public bool channel_points_voting_enabled { get; set; }
            public int channel_points_per_vote { get; set; }

            public enum PollStatus
            {
                @null,

                /*
ACTIVE — The poll is running.
COMPLETED — The poll ended on schedule (see the duration field).
TERMINATED — The poll was terminated before its scheduled end.
ARCHIVED — The poll has been archived and is no longer visible on the channel.
MODERATED — The poll was deleted.
INVALID — Something went wrong while determining the state.
                 */
                ACTIVE,
                COMPLETED,
                TERMINATED,
                ARCHIVED,
                MODERATED,
                INVALID
            }

            public PollStatus status { get; set; }
            public int duration { get; set; }
            public string started_at { get; set; }
            public string ended_at { get; set; }

            public class Page
            {
                public string cursor { get; set; }
            }

            public Page[] pagination { get; set; }
        }

        public class PollsPayload
        {
            public TwitchUserData[] data { get; set; }
        }

        public TwitchUserData StremerData;

        public class UsersPayload
        {
            public TwitchUserData[] data { get; set; }
        }

        public class TwitchUserData
        {
            public string broadcaster_type { get; set; }
            public string created_at { get; set; }
            public string description { get; set; }
            public string display_name { get; set; }
            public string id { get; set; }
            public string login { get; set; }
            public string offline_image_url { get; set; }
            public string profile_image_url { get; set; }
            public string type { get; set; }
            public long view_count { get; set; }
        }


        public override string GUID => "kopie.wildfrost.twitchextension";
        public override string[] Depends => Array.Empty<string>();
        public override string Title => "Twitch Mod";
        public override string Description => "Spice up the game with ur viewers on twitch.";
    }
}