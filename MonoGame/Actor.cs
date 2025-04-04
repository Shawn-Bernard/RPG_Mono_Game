﻿using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Nez.Sprites;
using Nez;
using Nez.Textures;
using System.Linq;
using Nez.Tweens;
using System.Linq.Expressions;
using Nez.AI.Pathfinding;
using System.Collections.Generic;

namespace MonoGame
{
    public class Actor : Entity
    {
        public SpriteRenderer spriteRenderer;
        public Entity TurnManager;
        public bool isTurn;
        public bool WaitForTurn;
        public bool WaitAnimation;

        public Texture2D entityTexture;

        public Vector2 startPosition;

        public Vector2 tilePosition;

        public Map map;

        public HealthSystem healthSystem;

        public TurnBasedSystem turnBasedSystem;

        public BoxCollider boxCollider;

        public List<Point> ActorsPosition = new List<Point>();
        public Actor(Vector2 vector2)
        {
            startPosition = vector2;
            AddComponent(new HealthSystem());

            healthSystem = GetComponent<HealthSystem>();
            AddComponent(new UI(this));
            AddComponent(new BoxCollider().SetSize(16,16));
        }

        public override void OnAddedToScene()
        {
            map = Scene.EntitiesOfType<Map>().FirstOrDefault();
            turnBasedSystem = Scene.FindComponentOfType<TurnBasedSystem>();
            
        }

        public AstarGridGraph Grid()
        {

            AstarGridGraph grid = new AstarGridGraph(500, 500);

            foreach (Actor actor in turnBasedSystem.Actors)
            {
                if (actor != this)
                ActorsPosition.Add(new Point((int)actor.Position.X, (int)actor.Position.Y));
            }

            foreach (var item in ActorsPosition)
            {
                grid.Dirs.Add(item);
            }
            
            return grid;
        }


        public void LoadTexture(string textureName)
        {
            entityTexture = Scene.Content.Load<Texture2D>(textureName);
            SpriteRenderer tileRenderer = new SpriteRenderer(entityTexture);
            tileRenderer.SetOrigin(Position);
            tileRenderer.SetLayerDepth(0);
            //Debug.Log(startPosition);
            //Debug.Log(startPosition / 16);
            AddComponent(tileRenderer);
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public virtual void StartTurn()
        {
            //Debug.Log("Started turn");
            isTurn = true;
            WaitForTurn = false;
            Scene.Camera.SetPosition(Position);
        }

        public virtual void EndTurn()
        {
            //Debug.Log("Ended turn");
            isTurn = false;
            WaitForTurn = true;
            turnBasedSystem.UpdateTurn();
        }

        public virtual void UpdateTurn()
        {
            if (WaitAnimation)
            {
                StartTurn();
            }
            else
            {
                EndTurn();
            }
        }

        public virtual void Attack(Actor actor)
        {
            actor.healthSystem.TakeDamage(1);
            Debug.Log("This actor is being attacked");
            Debug.Log(actor.Name);
            Debug.Log("This is the actor health now");
            Debug.Log(actor.healthSystem.health);

            EndTurn();
        }

        public void Move(Vector2 targetPosition)
        {
            if (Position != targetPosition)
            {
                Debug.Log(Name);
                WaitAnimation = true;
                Vector2 MoveVector = targetPosition;
                // moving to the move vector, how long the action is. then what to do after its done
                this.TweenPositionTo(MoveVector, 1.20f).SetCompletionHandler(action =>{WaitAnimation = false; UpdateTurn();}).Start();
                

            }
        }
    }
}
