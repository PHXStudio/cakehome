#pragma warning disable 0649

using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    [System.Serializable]
    public class PreloadedLevelData
    {
        [SerializeField] Tile[] tiles;
        public Tile[] Tiles => tiles;

        public void Init()
        {
            foreach(Tile tile in tiles)
            {
                tile.Init();
            }
        }

        [System.Serializable]
        public class Tile
        {
            [SerializeField] ElementPosition elementPosition;
            public ElementPosition ElementPosition => elementPosition;

            [SerializeField] int tileID;
            public int TileID => tileID;

            [SerializeField] TileEffectType effectType;
            public TileEffectType EffectType => effectType;

            private TileData tileData;
            public TileData TileData => tileData;

            public void Init()
            {
                tileData = LevelController.Database.GetTile(tileID);
            }
        }
    }
}