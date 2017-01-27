namespace RiverTrace
{
    interface ImageSource
    {
        byte[] GetTile(int tileIndexX, int tileIndexY, int zoom);
    }
}
