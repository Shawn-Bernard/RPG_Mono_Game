﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez;
using Nez.AI.BehaviorTrees;
using Nez.AI.GOAP;
using Nez.AI.Pathfinding;
using Nez.Sprites;
using Nez.Textures;
using Nez.Tiled;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static MonoGame.Actor;

namespace MonoGame
{
    public class Map : Entity, IUpdatable
    {

        public static Map instance;

        TurnBasedSystem turnBasedSystem;

        System.Random rng = new();

        private string path = $"{Environment.CurrentDirectory}/../../../Maps/";

        public Dictionary<Vector2, int> tileMap;

        int rngX;

        int rngY;

        private int lastItem;

        private List<string> Maps = new List<string>();

        private List<Item> ItemsInScene = new List<Item>();

        public List<Enemy> enemies = new List<Enemy>();

        Player player;
        //AstarGridGraph grid;

        Texture2D wallTexture, groundTexture, exitTexture;

        public int Level;
        public Map()
        {
            instance = this;
            Name = "Map";
            AddPreMadeMaps();
        }

        public override void OnAddedToScene()
        {
           
            //Debug.Log($"{Environment.CurrentDirectory}/../../../Maps/");
            

            turnBasedSystem = Scene.FindComponentOfType<TurnBasedSystem>();


            wallTexture = Scene.Content.Load<Texture2D>("Wall");
            groundTexture = Scene.Content.Load<Texture2D>("Ground");
            exitTexture = Scene.Content.Load<Texture2D>("Exit");
            
            MapStyle();

            AddListToScene();
            
        }

        public Enemy GetEnemy(Vector2 vector2)
        {
            foreach (Enemy enemy in enemies)
            {
                if (enemy.Position == vector2)
                {
                    if (enemy == null)
                    {
                        Debug.Log("No enemy was found");
                    }
                    return enemy;
                }
            }
            return null;
        }

        public bool CanLeave()
        {
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                Enemy enemy = enemies[i];
                if (enemy != null && enemy is Ghost)
                {
                    return true;
                }
            }
            return false;
        }
        public Item GetItem(string name)
        {

            var items = Scene.FindEntity(name);

            foreach (var item in ItemsInScene)
            {
                if (item.Name == name)
                {
                    return item;
                }
            }

            return null;
        }
        public void ReloadMap()
        {

            //turnBasedSystem.KillAllActor();
            turnBasedSystem.RemoveActor(player);

            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                Enemy enemy = enemies[i];
                if (enemy != null && enemy is Ghost)
                {
                    turnBasedSystem.RemoveActor(enemy);
                }
            }

            foreach (var item in ItemsInScene)
            {
                item.Destroy();
            }

            

            enemies = new List<Enemy>();

            ItemsInScene.Clear();
            tileMap.Clear();

            // Remove sprite components
            RemoveAllComponents();

            MapStyle();

            AddListToScene();
        }

        private void MapStyle()
        {
            Level++;
            //Level = 3;
            if (Level == 3)
            {
                tileMap = TextMap(path + "Level_Boss.txt");
            }
            else
            {
                int mapRPNG = rng.Next(0, 2);
                //Picking which map style to load
                if (mapRPNG == 0)
                {
                    tileMap = InitializeMap();
                }
                else
                {
                    //tileMap = InitializeMap();
                    tileMap = TextMap(path + PickRandomMap());
                }
                Debug.Log("!!!!Level!!!");
                Debug.Log(Level);
            }
            loadMap();

        }

        private Dictionary<Vector2, int> InitializeMap()
        {
            Dictionary<Vector2, int> MapGen = new Dictionary<Vector2, int>();
            rngX = rng.Next(15, 30);
            rngY = rng.Next(15, 30);

            //Step 1: initializing map
            for (int x = 0; x < rngX; x++)
            {
                for (int y = 0; y < rngY; y++)
                {
                    //Adding my base map with only floors
                    MapGen.Add(new Vector2(x, y), 1);
                }
            }

            //Step 2: placing walls
            for (int x = 0; x < rngX; x++)
            {
                for (int y = 0; y < rngY; y++)
                {
                    //If statement that checks around the borders
                    if (x == 0 || y == 0 || x == rngX - 1 || y == rngY - 1)
                    {
                        MapGen[new Vector2(x, y)] = 0;
                    }
                }
            }

            //Step 3: Place clusters
            int clusterCount = MapGen.Count;
            for (int i = 0; i < clusterCount; i++)
            {
                int clusterX = rng.Next(1, rngX - 2);
                int clusterY = rng.Next(1, rngY - 2);
                int clusterWidth = rng.Next(2, 4);
                int clusterHeight = rng.Next(2, 4);

                bool canPlace = true;

                for (int x = -1; x < clusterWidth + 1; x++)
                {
                    for (int y = -1; y < clusterHeight + 1; y++)
                    {
                        Vector2 checkPosition = new Vector2(clusterX + x, clusterY + y);
                        if (MapGen.ContainsKey(checkPosition) && MapGen[checkPosition] == 0)
                        {
                            canPlace = false;
                        }
                    }
                }

                if (canPlace)
                {
                    for (int x = 0; x < clusterWidth; x++)
                    {
                        for (int y = 0; y < clusterHeight; y++)
                        {
                            Vector2 ClusterPosition = new Vector2(clusterX + x, clusterY + y);
                            MapGen[ClusterPosition] = 0;
                        }
                    }
                }
            }

            bool playerPlaced = false;
            Vector2 playerPosition = Vector2.Zero;

            //Is the player placed
            while (!playerPlaced)
            {
                int playerX = rng.Next(1, rngX - 2);
                int playerY = rng.Next(1, rngY - 2);
                Vector2 checkPosition = new Vector2(playerX, playerY);

                if (MapGen[checkPosition] == 1)
                {
                    MapGen[checkPosition] = 3;
                    playerPosition = checkPosition;
                    playerPlaced = true;
                }
            }

            // A method to see if the position we want if to close to the target position
            bool IsTooClose(Vector2 position, Vector2 targetPosition)
            {
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        Vector2 checkPosition = new Vector2(position.X + x, position.Y + y);
                        if (checkPosition == targetPosition)
                        {
                            return true;//returns true if too close
                        }
                    }
                }
                return false;
            }

            int EnemyCount = rng.Next(1, 3);
            for (int EnemyPlaced = 0; EnemyPlaced < EnemyCount; EnemyPlaced++)
            {
                bool enemyPlaced = false;
                while (!enemyPlaced)
                {
                    int enemyX = rng.Next(2, rngX - 2);
                    int enemyY = rng.Next(2, rngY - 2);
                    Vector2 enemyPosition = new Vector2(enemyX, enemyY);

                    if (MapGen[enemyPosition] == 1 && !IsTooClose(enemyPosition, playerPosition))
                    {
                        MapGen[enemyPosition] = 4;
                        enemyPlaced = true;
                    }
                }
            }

            // Repeat the same for Ghost and Spider
            int GhostCount = rng.Next(1, 2);
            for (int EnemyPlaced = 0; EnemyPlaced < GhostCount; EnemyPlaced++)
            {
                bool GhostPlaced = false;
                while (!GhostPlaced)
                {
                    int enemyX = rng.Next(2, rngX - 2);
                    int enemyY = rng.Next(2, rngY - 2);
                    Vector2 enemyPosition = new Vector2(enemyX, enemyY);

                    if (MapGen[enemyPosition] == 1 && !IsTooClose(enemyPosition, playerPosition))
                    {
                        MapGen[enemyPosition] = 5;
                        GhostPlaced = true;
                    }
                }
            }

            int SpiderCount = rng.Next(1, 2);
            for (int EnemyPlaced = 0; EnemyPlaced < SpiderCount; EnemyPlaced++)
            {
                bool SpiderPlaced = false;
                while (!SpiderPlaced)
                {
                    int enemyX = rng.Next(2, rngX - 2);
                    int enemyY = rng.Next(2, rngY - 2);
                    Vector2 enemyPosition = new Vector2(enemyX, enemyY);

                    if (MapGen[enemyPosition] == 1 && !IsTooClose(enemyPosition, playerPosition))
                    {
                        MapGen[enemyPosition] = 6;
                        SpiderPlaced = true;
                    }
                }
            }

            // Placing exit door
            bool exitPlaced = false;
            while (!exitPlaced)
            {
                int doorX = rng.Next(1, rngX - 2);
                int doorY = rng.Next(1, rngY - 2);
                Vector2 checkPosition = new Vector2(doorX, doorY);

                if (MapGen[checkPosition] == 1)
                {
                    MapGen[checkPosition] = 2;
                    exitPlaced = true;
                }
            }

            // Placing random items
            int itemCount = 12;
            for (int i = 0; i < itemCount; i++)
            {
                int rngItem;
                bool itemPlaced = false;
                while (!itemPlaced)
                {
                    int itemX = rng.Next(1, rngX - 2);
                    int itemY = rng.Next(1, rngY - 2);

                    do
                    {
                        rngItem = rng.Next(7, 10);
                    }
                    while (rngItem == lastItem);
                    Vector2 itemPosition = new Vector2(itemX, itemY);

                    if (MapGen[itemPosition] == 1)
                    {
                        MapGen[itemPosition] = rngItem;
                        lastItem = rngItem;
                        itemPlaced = true;
                    }
                }
            }

            return MapGen;
        }


        // Adding my list of strings for my map
        private void AddPreMadeMaps()
        {
            Maps.Add("Level_1.txt");
            Maps.Add("Level_2.txt");
            Maps.Add("Level_3.txt");
        }

        public int checkTile(Vector2 checkedPosition)
        {
            if (tileMap.ContainsKey(checkedPosition))
            {
                return tileMap[checkedPosition];// Return the tile value if it there
            }
            else
            {
                return 0; // Return a default value 
            }
        }

        //Picks a random map from the list of maps 
        private string PickRandomMap()
        {
            rng = new System.Random();
            int index = rng.Next(Maps.Count);
            return Maps[index];
        }
        private Dictionary<Vector2, int> TextMap(string filepath)
        {
            Dictionary<Vector2, int> result = new Dictionary<Vector2, int>();
            StreamReader reader = new StreamReader(filepath);
            int y = 0;
            string line;

            //This will give line the value untill the reader is done reading the text file
            while ((line = reader.ReadLine()) != null)
            {
                for (int x = 0; x < line.Length; x++)
                {
                    Vector2 tilePosition = new Vector2(x,y); // Correct y indexing
                    char tile = line[x];
                    switch (tile)
                    {
                        case '#':
                            result[tilePosition] = 0; // Walls
                            break;
                        case '-':
                            result[tilePosition] = 1; // Floor
                            break;
                        case '*':
                            result[tilePosition] = 2; // Exit
                            break;
                        case '@':
                            result[tilePosition] = 3; // Player
                            break;
                        case '=':
                            result[tilePosition] = 4; // Enemy
                            break;
                        case '$':
                            result[tilePosition] = 5; // Ghost
                            break;
                        case '.':
                            result[tilePosition] = 6; // Spider
                            break;
                        case '+':
                            result[tilePosition] = 7; // Health
                            break;
                        case 'F':
                            result[tilePosition] = 8; // FireBall
                            break;
                        case 'L':
                            result[tilePosition] = 9; // Lightninhg 
                            break;
                        case '!':
                            result[tilePosition] = 10; // BOSS
                            break;
                    }
                }
                y++;
            }
            return result;
        }
        public void loadMap()
        {
            // The result is the return from Text Map
            foreach (var Result in tileMap)
            {
                Vector2 tilePosition = new Vector2(Result.Key.X, Result.Key.Y);

                switch (Result.Value)
                {
                    case 0:
                        addTile(wallTexture, tilePosition);
                        break;
                    case 1:
                        addTile(groundTexture, tilePosition);
                        break;
                    case 2:
                        addTile(exitTexture, tilePosition);
                        break;
                    case 3:
                        Player oldPlayer = player; // save the current player (if any)

                        player = new Player();
                        player.startPosition = tilePosition;

                        if (oldPlayer != null)
                        {
                            // Copy health
                            player.healthSystem.health = oldPlayer.healthSystem.health;

                            // Clone inventory and update ownership
                            player.Inventory = new List<Item>();
                            foreach (Item item in oldPlayer.Inventory)
                            {
                                item.Owner = player; // Set new owner
                                player.Inventory.Add(item); // Add to new player's inventory
                            }
                        }

                        turnBasedSystem.AddActor(player);
                        Scene.AddEntity(player);
                        addTile(groundTexture, tilePosition);
                        break;
                    case 4:
                        Wizard wizard = new Wizard();
                        wizard.startPosition = tilePosition;
                        turnBasedSystem.AddActor(wizard);
                        enemies.Add(wizard);
                        addTile(groundTexture, tilePosition);
                        break;
                    case 5:
                        Ghost ghost = new Ghost();
                        ghost.startPosition = tilePosition;
                        turnBasedSystem.AddActor(ghost);
                        enemies.Add(ghost);
                        addTile(groundTexture, tilePosition);
                        break;
                    case 6:
                        Spider spider = new Spider();
                        spider.startPosition = tilePosition;
                        turnBasedSystem.AddActor(spider);
                        enemies.Add(spider);
                        addTile(groundTexture, tilePosition);
                        break;
                    case 7:
                        HealingPotion potion = new HealingPotion();
                        potion.tilePosition = tilePosition;
                        ItemsInScene.Add(potion);
                        addTile(groundTexture, tilePosition);
                        break;
                    case 8:
                        ScrollOfFireball fireball = new ScrollOfFireball();
                        fireball.tilePosition = tilePosition;
                        ItemsInScene.Add(fireball);
                        addTile(groundTexture, tilePosition);
                        break;
                    case 9:
                        ScrollOfLightning lightning = new ScrollOfLightning();
                        lightning.tilePosition = tilePosition;
                        ItemsInScene.Add(lightning);
                        addTile(groundTexture, tilePosition);
                        break;
                    case 10:
                        Boss boss = new Boss();
                        boss.startPosition = tilePosition;
                        turnBasedSystem.AddActor(boss);
                        enemies.Add(boss);
                        addTile(groundTexture, tilePosition);
                        break;
                }
            }
            
        }
        public void addTile(Texture2D texture, Vector2 position)
        {
            SpriteRenderer tileRenderer = new SpriteRenderer(texture);
            tileRenderer.SetOrigin(-position * 16);
            tileRenderer.SetLayerDepth(1);
            //Debug.Log(tileRenderer.Sprite);
            //Debug.Log(Scale);
            AddComponent(tileRenderer);
        }
        private void AddListToScene()
        {
            foreach (Enemy actor in enemies)
            {
                Debug.Log($"enemy actor position!!!!!!!!!!!!!");
                Debug.Log(actor.Position);
                Scene.AddEntity(actor);
                //Debug.Log(actor.Name);
            }
            Debug.Log($"Enemy Count : {enemies.Count}");
            foreach (Item item in ItemsInScene)
            {
                Scene.AddEntity(item);
            }
            turnBasedSystem.UpdateTurn();
        }

        public void RemoveEnemy(Enemy enemy)
        {
            enemies.Remove(enemy);
        }
    }
}
