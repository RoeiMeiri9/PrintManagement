namespace PrinterManager.Resources
{
    public record Settings(string Value)
    {
        public static readonly Settings ScrollIndicatorSetting = new("ShowScrollDirectionIndicator");
    }
}
