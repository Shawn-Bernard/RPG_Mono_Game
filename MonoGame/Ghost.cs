﻿using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Nez.Sprites;
using Nez;
using Nez.AI.Pathfinding;
using Nez.Textures;
using System.Linq;
using Nez.AI.UtilityAI;
using System.IO;
using System.Collections.Generic;

namespace MonoGame
{
    public class Ghost: Enemy
    {
        public Ghost()
        {
            //Position = startPosition;
            Name = "Ghost";
            
        }
        public override void OnAddedToScene()
        {
            AddComponent(new GhostMovement(this));
            LoadTexture("Ghost");
        }

        public override void TakeDamage(int damage, AttackType attackType)
        {
            if (attackType == AttackType.Magic)
            {
                base.TakeDamage(damage, attackType);
            }
            else
            {
                Debug.Log("I'm a ghost and cannot be attack");
                return;
            }
        }

    }

    public class GhostMovement : EnemyMovement
    {
        public GhostMovement(Enemy actor) : base(actor)
        {
            entity = actor;
            tilePosition = entity.startPosition;
        }

        public override void Controller()
        {
            Vector2 targetPosition = tilePosition;

            player = (Player)entity.Scene.FindEntity("Player");
            // Getting tile position for my points 
            Point entityPosition = new Point((int)(entity.Position.X / 16), (int)(entity.Position.Y / 16));
            Point playerPosition = new Point((int)(player.Position.X / 16), (int)(player.Position.Y / 16));

            // Making my grind how for looking. this should be from my tile map but this is a quick fix 
            AstarGridGraph grid = new AstarGridGraph(500, 500);

            // Getting the path for the searched path to and from
            List<Point> path = grid.Search(entityPosition, playerPosition);

            if (path != null && path.Count > 1)
            {
                // Getting the value of point that is a vector  
                Point nextStep = path[1];
                if (nextStep == playerPosition)
                {
                    entity.basicAttack(player);
                }
                else
                {
                    targetPosition = new Vector2(nextStep.X, nextStep.Y);

                    InteractOrMove(targetPosition);
                }
            }
            else
            {
                targetPosition = MoveInRandomDirection();

                InteractOrMove(targetPosition);
            }
        }

        // Ghosts just move freely, no walls should be checked
        public override void InteractOrMove(Vector2 targetPosition)
        {
            tilePosition = targetPosition;

            entity.Move(targetPosition * 16); // Move the ghost freely without wall collision
        }
    }
}
