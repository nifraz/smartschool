using HotChocolate;
using HotChocolate.Types;

namespace SmartSchool.Graphql.Subscriptions;

/// <summary>
/// Generic real-time subscription — one event type for every resource.
/// Topic is the resource key (e.g. "school"), published by GenericMutation
/// via ITopicEventSender after every create / update / delete.
/// </summary>
[ExtendObjectType<SchoolSubscription>]
public class GenericSubscription
{
    [Subscribe]
    [Topic("{resource}")]
    public ResourceChangedEvent ResourceChanged(
        string resource,
        [EventMessage] ResourceChangedEvent e) => e;
}

/// <summary>Event payload pushed to all subscribers of a resource topic.</summary>
public sealed record ResourceChangedEvent(
    string Resource,
    string Event,  // "created" | "updated" | "deleted"
    long Id);
