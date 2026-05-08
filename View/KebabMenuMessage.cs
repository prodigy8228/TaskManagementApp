using CommunityToolkit.Mvvm.Messaging.Messages;

namespace TaskManagement.View;

public class KebabMenuMessage : ValueChangedMessage<string>
{
    public KebabMenuMessage(string value) : base(value) { }
}
