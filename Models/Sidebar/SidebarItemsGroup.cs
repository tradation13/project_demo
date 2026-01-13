namespace IPTS.Models.Sidebar
{
    public class SidebarItemsGroup
    {
        public string Title { get; set; } = string.Empty;
        public required List<SidebarItem> Items { get; set; }
        public List<string> Roles { get; set; } = [];

    }
}
