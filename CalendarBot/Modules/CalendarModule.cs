using LiteDB;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CalendarBot
{
    public sealed class CalendarModule : InteractionModuleBase<InteractionContext<DiscordSocketClient>>
    {
        public ILiteCollection<CalendarEvent> Events { get; set; }
        public CultureInfo Culture { get; set; }

        [SlashCommand("ping", "recieve a pong")]
        public async Task Ping() =>
            await RespondAsync("pong");

        [SlashCommand("latency", "Get the estimate round-trip latency to the gateway server")]
        public async Task Latency() =>
            await RespondAsync(Context.Client.Latency.ToString() + " ms", ephemeral: true);

        [SlashCommand("invite", "Get the invite link")]
        public async Task Invite()
        {
            var button = new ComponentBuilder().WithButton("Invite Me!", style: ButtonStyle.Link,
                url: $"https://discord.com/api/oauth2/authorize?client_id={Context.Client.CurrentUser.Id}&permissions=2048&redirect_uri=https%3A%2F%2Fhydrametry.com&scope=bot%20applications.commands")
                .Build();

            await RespondAsync("\u200B", component: button, ephemeral: true);
        }

        // Create a calendar event
        [SlashCommand("create", "create a calendar event")]
        public async Task Create(string name, string description,
            [InclusiveRange(1, 31)] int day,
            Months month,
            [InclusiveRange(0, 9999)] int year,
            [InclusiveRange(0, 23)] int hour,
            [InclusiveRange(0, 59)] int minute,
            RecursionInterval recursionInterval = RecursionInterval.None)
        {
            // Create event guid
            var guid = Guid.NewGuid();

            // Create event object using the guid
            var newEvent = new CalendarEvent {
                Id = guid,
                Name = name,
                Description = description,
                CreatedAt = DateTime.Now,
                DateAndTime = new DateTime(year, (int)month, day, hour, minute, 0, DateTimeKind.Local),
                Color = Color.Orange,
                MessageChannelId = Context.Channel.Id,
                GuildId = Context.Guild.Id,
                RecursionInterval = recursionInterval,
                UserId = Context.User.Id
            };

            // Inser the event to database collection
            Events.Insert(newEvent);

            // Print target configuration dialog for the event
            await PrintTargetConfigure(newEvent, "add");
        }

        [Group("print", "display event information")]
        public class Print : InteractionModuleBase<InteractionContext<DiscordSocketClient>>
        {
            public ILiteCollection<CalendarEvent> Events { get; set; }
            public CultureInfo Culture { get; set; }

            // Print every event that is scheduled to a user-input day
            [SlashCommand("day", "Print the events of a day")]
            public async Task Day([InclusiveRange(1, 31)] int day, Months month, [InclusiveRange(1, 9999)] int year)
            {
                // Parse the user input
                var targetDate = new DateTime(year, (int)month, day);

                // Find every event and order them from earliest to latest
                var events = Events.Find(x => x.DateAndTime.Date == targetDate).OrderBy(x => x.DateAndTime);

                // Create an embed to print the events
                var embed = EmbedUtility.FromPrimary($"Upcoming Server Events [{targetDate.ToString(Culture.DateTimeFormat.ShortDatePattern)}]", null, builder => {
                    // If events collection is empty dont add embed field
                    if (!events.Any()) {
                        builder.Description = "No events found";
                    }
                    // Else create embed field for every event
                    else {
                        foreach (var ev in events)
                            builder.AddField(ev.Name + $" - **{ev.DateAndTime.ToString(Culture.DateTimeFormat.ShortTimePattern)}**", ev.Description);
                    }
                });

                // Create a dropdown menu for selecting an event
                var component = events.Any() ? new ComponentBuilder().WithSelectMenu("event-configure",
                    events.Select(x => new SelectMenuOptionBuilder(x.Name, x.Id.ToString(), x.Id.ToString())).ToList()).Build() : null;

                // Send the response
                await RespondAsync(embed: embed, component: component, ephemeral: true);
            }

            // Get event info using an Autocompleter
            [SlashCommand("event", "Print the info of an event")]
            public async Task Event([Autocomplete(typeof(EventAutocompleter))] Guid guid) =>
                await PrintConfigureEventDialog(guid);

            // Capture dropdown menu selection from "print day" command
            [ComponentInteraction("event-configure", true)]
            public async Task ConfigureEvent(string[] values) =>
                await PrintConfigureEventDialog(Guid.Parse(values[0]));

            // Create event info dialog
            public async Task PrintConfigureEventDialog(Guid guid)
            {
                var ev = Events.FindById(guid);

                if (ev is null) {
                    await RespondAsync(embed: EmbedUtility.FromError(null, "Event no longer exists.", false));
                    return;
                }

                var embed = EmbedUtility.FromEvent(ev, Context.Client);

                var component = new ComponentBuilder()
                    .WithButton("Change Date", $"event-change-date:{ev.Id}", ButtonStyle.Primary, (Emoji)":calendar:")
                    .WithButton("Change Time", $"event-change-time:{ev.Id}", ButtonStyle.Primary, (Emoji)":clock1:")
                    .WithButton("Add Targets", $"event-add-targets:{ev.Id}", ButtonStyle.Primary, (Emoji)":loudspeaker:")
                    .WithButton("Remove Targets", $"event-remove-targets:{ev.Id}", ButtonStyle.Primary, (Emoji)":loudspeaker:")
                    .WithButton("Delete", $"event-delete:{ev.Id}", ButtonStyle.Danger, (Emoji)":wastebasket:")
                    .Build();

                await RespondAsync(embed: embed, component: component, ephemeral: true);
            }
        }

        // Change the date of an event
        [SlashCommand("change-date", "Change the date of an event")]
        public async Task ChangeDate([Autocomplete(typeof(EventAutocompleter))] Guid guid, [InclusiveRange(1, 31)] int day, Months month, [InclusiveRange(1, 9999)] int year)
        {
            // Fetch the event
            var ev = Events.FindById(guid);

            // Cache the time to re-use it when creating the new DateTime
            var time = ev.DateAndTime.TimeOfDay;

            // Replace the date property of the event
            ev.DateAndTime = new DateTime(year, (int)month, day, time.Hours, time.Minutes, time.Seconds);

            // Try to update the event database collection
            if (Events.Update(ev))
                await RespondAsync(embed: EmbedUtility.FromSuccess(null, "Successfully updated the event", false), ephemeral: true);
            else
                await RespondAsync(embed: EmbedUtility.FromError(null, "There was a problem", false), ephemeral: true);
        }

        // Change the time of an event
        [SlashCommand("change-time", "Change the time of an event")]
        public async Task ChangeTime([Autocomplete(typeof(EventAutocompleter))] Guid guid, [InclusiveRange(0, 23)] int hour, [InclusiveRange(0, 59)] int minute)
        {
            // Fetch the event
            var ev = Events.FindById(guid);

            // Cache the date to re-use it when creating the new DateTime
            var date = ev.DateAndTime.Date;

            // Replace the time property of the event
            ev.DateAndTime = new DateTime(date.Year, date.Month, date.Day, hour, minute, 0);

            // Try to update the event database collection
            if (Events.Update(ev))
                await RespondAsync(embed: EmbedUtility.FromSuccess(null, "Successfully updated the event", false), ephemeral: true);
            else
                await RespondAsync(embed: EmbedUtility.FromError(null, "There was a problem", false), ephemeral: true);
        }

        // Print a calendar page image
        [SlashCommand("calendar-page", "Display a calendar page")]
        [Acknowledge(true)]
        public async Task Test(Months month, [InclusiveRange(1, 9999)]int year)
        {
            // Create the bitmap
            using var bmp = CalendarUtility.GenerateCalendarPage(1200, 800, (int)month, year);

            // Create a memory stream
            using var ms = new MemoryStream();

            // Save bitmap to memory stream
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);

            // Reset the stream offset
            ms.Seek(0, SeekOrigin.Begin);

            // Send a followup message to the user, containg the image
            var message = await Context.Interaction.FollowupWithFileAsync(ms, "cal.png",
                embed: EmbedUtility.FromPrimary($":calendar_spiral: {month}, {year}", null, builder => builder.ImageUrl = "attachment://cal.png"), ephemeral: true);
        }

        // Rename an event
        [SlashCommand("rename", "Change the name of an event")]
        public async Task Rename([Autocomplete(typeof(EventAutocompleter))] Guid guid, string newName)
        {
            // Fetch the event
            var ev = Events.FindById(guid);

            // Replace its name
            ev.Name = newName;

            // Try to update the event database
            if (Events.Update(ev))
                await RespondAsync(embed: EmbedUtility.FromSuccess(null, "Successfully updated the event", false), ephemeral: true);
            else
                await RespondAsync(embed: EmbedUtility.FromError(null, "There was a problem", false), ephemeral: true);
        }

        // Delete an event
        [SlashCommand("delete", "Delete an event")]
        public async Task Delete([Autocomplete(typeof(EventAutocompleter))] Guid guid)
        {
            // Try to delete the event from the database
            if (Events.Delete(guid))
                await RespondAsync(embed: EmbedUtility.FromSuccess(null, "Event deleted.", false), ephemeral: true);
            else
                await RespondAsync(embed: EmbedUtility.FromError(null, "Event couln't be deleted.", false), ephemeral: true);
        }

        // Handle "Delete" button presses
        [ComponentInteraction("event-delete:*")]
        public async Task Delete(string guid)
        {
            // Try to delete the event from the database
            if (Events.Delete(Guid.Parse(guid)))
                await (Context.Interaction as SocketMessageComponent).UpdateAsync(props => {
                    props.Content = string.Empty;
                    props.Embeds = null;
                    props.Components = null;
                    props.Embed = EmbedUtility.FromSuccess(null, "Event deleted.", false);
                });
            else
                await (Context.Interaction as SocketMessageComponent).UpdateAsync(props => {
                    props.Content = string.Empty;
                    props.Embeds = null;
                    props.Components = null;
                    props.Embed = EmbedUtility.FromError(null, "Event couln't be deleted.", false);
                });
        }

        // Handle "Configure Target Roles" dropdown selections
        [ComponentInteraction("event-target-role:*,*")]
        [Acknowledge]
        public async Task ConfigureRoles(string guid, string op, params string[] values)
        {
            // Convert selection ids to ulongs
            var roles = values.Select(x => Convert.ToUInt64(x));

            // Fetch the event
            var ev = Events.FindById(Guid.Parse(guid));

            // Check if event exists
            if (ev is null) {
                await FollowupAsync(embed: EmbedUtility.FromError(null, "Event no longer exists.", false));
                return;
            }

            // Check if there is already a TargetRoles collection in db
            if (ev.TargetRoles is not null)
                // if there is, perform op
                switch (op) {
                    case "add":
                        ev.TargetUsers.AddRange(roles);
                        break;
                    case "remove":
                        ev.TargetUsers.RemoveAll(x => roles.Contains(x));
                        break;
                }
            else {
                // else, create a new collection if op is "add"
                if (op == "add") {
                    ev.TargetUsers = new List<ulong>();
                    ev.TargetUsers.AddRange(roles);
                }
                // "remove" is no op, since there is no existing collection
            }

            // Update the database
            Events.Update(ev);
        }

        // Handle "Configure Target Roles" dropdown selections
        [ComponentInteraction("event-target-user:*,*")]
        [Acknowledge]
        public async Task ConfigureUsers(string guid, string op, params string[] values)
        {
            // Convert selection ids to ulongs
            var users = values.Select(x => Convert.ToUInt64(x));

            // Fetch the event
            var ev = Events.FindById(Guid.Parse(guid));

            // Check if event exists
            if (ev is null) {
                await FollowupAsync(embed: EmbedUtility.FromError(null, "Event no longer exists.", false));
                return;
            }

            // Check if there is already a TargetUsers collection in db
            if (ev.TargetUsers is not null)
                // if there is, perform op
                switch (op) {
                    case "add":
                        ev.TargetUsers.AddRange(users);
                        break;
                    case "remove":
                        ev.TargetUsers.RemoveAll(x => users.Contains(x));
                        break;
                }
            else {
                // else, create a new collection if op is "add"
                if (op == "add") {
                    ev.TargetUsers = new List<ulong>();
                    ev.TargetUsers.AddRange(users);
                }
                // "remove" is no op, since there is no existing collection
            }

            // Update the database
            Events.Update(ev);
        }

        // Handle "dismiss" button presses
        [ComponentInteraction("dismiss")]
        public async Task Dismiss()
        {
            // Clear the message, the button press originated from
            await (Context.Interaction as SocketMessageComponent).UpdateAsync(props => {
                props.Content = string.Empty;
                props.Components = null;
                props.Embeds = null;
                props.Embed = EmbedUtility.FromSuccess(null, "Message Dismissed", false);
            });
        }

        // Handle "AddTargets"/"RemoveTargets" button presses
        [ComponentInteraction("event-*-targets:*")]
        public async Task ChangeTargets(string op, string guid)
        {
            // Fetch the event
            var ev = Events.FindById(Guid.Parse(guid));

            // Print configuration dialog
            await PrintTargetConfigure(ev, op);
        }

        // Handle "Change Date" button presses
        [ComponentInteraction("event-change-date:*")]
        public async Task ChangeDateDialog(string guid)
        {
            // Fetch the event
            var ev = Events.FindById(Guid.Parse(guid));

            // Check if event exists
            if (ev is null) {
                await RespondAsync(embed: EmbedUtility.FromError(null, "Event no longer exists.", false));
                return;
            }

            // Create add/substract buttons, pass the offset to button ids, in hours
            var component = new ComponentBuilder()
                .WithButton("1 Day", $"event-time-add-24:{guid}", ButtonStyle.Primary, (Emoji)":heavy_plus_sign:")
                .WithButton("1 Week", $"event-time-add-{TimeSpan.FromDays(7).TotalHours}:{guid}", ButtonStyle.Primary, (Emoji)":heavy_plus_sign:")
                .WithButton("1 Month", $"event-time-add-{TimeSpan.FromDays(31).TotalHours}:{guid}", ButtonStyle.Primary, (Emoji)":heavy_plus_sign:")
                .WithButton("1 Day", $"event-time-substract-24:{guid}", ButtonStyle.Primary, (Emoji)":heavy_minus_sign:", row: 1)
                .WithButton("1 Week", $"event-time-substract-{TimeSpan.FromDays(7).TotalHours}:{guid}", ButtonStyle.Primary, (Emoji)":heavy_minus_sign:", row: 1)
                .WithButton("1 Month", $"event-time-substract-{TimeSpan.FromDays(31).TotalHours}:{guid}", ButtonStyle.Primary, (Emoji)":heavy_minus_sign:", row: 1)
                .Build();

            // Send the response
            await RespondAsync(embed: EmbedUtility.FromPrimary($"Configure {ev.Name} event", null), component: component, ephemeral: true);
        }

        // Handle "Change Date" button presses
        [ComponentInteraction("event-change-time:*")]
        public async Task ChangeTimeDialog(string guid)
        {
            // Fetch the event
            var ev = Events.FindById(Guid.Parse(guid));

            // Check if event exists
            if (ev is null) {
                await RespondAsync(embed: EmbedUtility.FromError(null, "Event no longer exists.", false));
                return;
            }

            // Create add/substract buttons, pass the offset to button ids, in hours
            var component = new ComponentBuilder()
                .WithButton("1 Hour", $"event-time-add-1:{guid}", ButtonStyle.Primary, (Emoji)":heavy_plus_sign:")
                .WithButton("4 Hours", $"event-time-add-4:{guid}", ButtonStyle.Primary, (Emoji)":heavy_plus_sign:")
                .WithButton("12 Hours", $"event-time-add-12:{guid}", ButtonStyle.Primary, (Emoji)":heavy_plus_sign:")
                .WithButton("1 Hour", $"event-time-substract-1:{guid}", ButtonStyle.Primary, (Emoji)":heavy_minus_sign:", row: 1)
                .WithButton("4 Hours", $"event-time-substract-4:{guid}", ButtonStyle.Primary, (Emoji)":heavy_minus_sign:", row: 1)
                .WithButton("12 Hours", $"event-time-substract-12:{guid}", ButtonStyle.Primary, (Emoji)":heavy_minus_sign:", row: 1)
                .Build();

            // Send the response
            await RespondAsync(embed: EmbedUtility.FromPrimary($"Configure {ev.Name} event", null), component: component, ephemeral: true);
        }

        // Handle "+/- time" button presses
        [ComponentInteraction("event-time-*-*:*")]
        [Acknowledge]
        public Task ChangeDateTime(string op, string hours, string guid)
        {
            // Fetch the event
            var ev = Events.FindById(Guid.Parse(guid));

            // Get the time offset from custom id string
            var hoursInt = Convert.ToInt32(hours);

            // Perfom addition/substraction operation
            switch (op) {
                case "add":
                    ev.DateAndTime += TimeSpan.FromHours(hoursInt);
                    break;
                case "substract":
                    ev.DateAndTime -= TimeSpan.FromHours(hoursInt);
                    break;
            }

            // Update the database
            Events.Update(ev);
            return Task.CompletedTask;
        }

        // Print role configuration dialog
        private async Task PrintTargetConfigure(CalendarEvent ev, string suffix)
        {
            // Get guild roles and guild members
            var roles = Context.Guild.Roles;
            var users = Context.Guild.Users;

            // Create a dropdown for selecting roles, push the role ids to menu item custom ids
            var targetRoleSelector = new SelectMenuBuilder($"event-target-role:{ev.Id},{suffix}", placeholder: "Select target roles",
                options: roles.Select(x => new SelectMenuOptionBuilder(x.Name, x.Id.ToString(), isDefault: ev.TargetRoles?.Contains(x.Id))).ToList(),
                maxValues: roles.Count);

            // Create a dropdown for selecting users, push the user ids to menu item custom ids
            var targetUserSelector = new SelectMenuBuilder($"event-target-user:{ev.Id},{suffix}", placeholder: "Select target users",
                options: users.Select(x => new SelectMenuOptionBuilder(x.Username, x.Id.ToString(), isDefault: ev.TargetUsers?.Contains(x.Id))).ToList(),
                maxValues: users.Count);

            // Crate the message component with a dismiss button
            var component = new ComponentBuilder()
                .WithSelectMenu(targetRoleSelector, 0)
                .WithSelectMenu(targetUserSelector, 1)
                .WithButton("Dismiss", $"dismiss", ButtonStyle.Secondary, emote: (Emoji)":heavy_multiplication_x:", row: 2)
                .Build();

            // Send the response
            await RespondAsync(embed: EmbedUtility.FromEvent(ev, Context.Client), component: component, ephemeral: true);
        }
    }
}