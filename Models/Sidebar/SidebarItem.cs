namespace IPTS.Models.Sidebar
{
    public class SidebarItem
    {
        public string Title { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Icon {  get; set; } = string.Empty;
        public string Controller { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = [];
        public List<SidebarItem>? SubSidebarItems { get; set; }
        
    }
}
