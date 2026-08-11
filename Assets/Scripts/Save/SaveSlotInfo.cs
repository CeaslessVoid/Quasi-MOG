namespace Save
{
    [System.Serializable]
    public struct SaveSlotInfo
    {
        public int slotIndex;
        public bool hasSave;
        public string saveName;
        public string lastPlayedUtc;
    }
}
