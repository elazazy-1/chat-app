using MauiApp3.Features.Chat.Group;
using MauiApp3.Features.Chat.Private;

namespace MauiApp3
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("GroupChatPage", typeof(GroupChatPage));
            Routing.RegisterRoute("PrivateChatPage", typeof(PrivateChatPage));
        }
    }
}
