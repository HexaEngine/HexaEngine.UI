namespace HexaEngine.UI
{
    using System;
    using System.Collections.ObjectModel;

    internal sealed class ResourceDictionaryCollection : Collection<ResourceDictionary>
    {
        private readonly ResourceDictionary owner;

        public ResourceDictionaryCollection(ResourceDictionary owner)
        {
            this.owner = owner;
        }

        protected override void InsertItem(int index, ResourceDictionary item)
        {
            ArgumentNullException.ThrowIfNull(item);
            owner.ValidateMergedDictionary(item);
            base.InsertItem(index, item);
        }

        protected override void SetItem(int index, ResourceDictionary item)
        {
            ArgumentNullException.ThrowIfNull(item);
            owner.ValidateMergedDictionary(item);
            base.SetItem(index, item);
        }
    }
}
