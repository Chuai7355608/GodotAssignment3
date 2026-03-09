using Godot;
using System;

public partial class PongLogic : Node
{

    [Export]
    public CpuParticles3D effects;
    [Export]
    public Camera3D camera;
    
    [Export]
    public CollisionShape3D cameraMoveArea;
    

    [Export]
    public Node3D leftPaddle;

    [Export]
    public Node3D rightPaddle;
    
    [Export]public Node3D L_car;
    [Export]public Node3D R_car;
    [Export]public Node3D LP;
    [Export]public Node3D RP;
    
    [Export]
    public float PaddleCollisionWidth = 0.2f;

    [Export]
    public Node3D ball;

    [Export]
    public Vector2 tableSize;

    [Export]
    public AudioStreamPlayer3D bounce;

    [Export]
    public AudioStreamPlayer3D fire;
    [Export]
    public AudioStreamPlayer3D music;

    private Vector3 ballVelocity = Vector3.Zero;

    [Export]
    private float ballSpeed = 5.0f; 
    [Export]
    public float ballHeight = 0.16f;
    [Export]
    public float ballPeakHeight = 1.0f;

    [Export]
    private float paddleSpeed = 10.0f; 

    [Export]
    public float paddleLerpSpeed = 20;
    
    [Export]
    public AnimationPlayer _animPlayerA;
    [Export]
    public AnimationPlayer _animPlayerB;

    [Export]
    public AnimationPlayer BounceA;
    [Export]
    public AnimationPlayer BounceB;

    private float parabolaA;
    private float leftPaddleX;
    private float rightPaddleX;

    private Random random = new Random();

    public float leftStickMagnitude = 0;
    public float rightStickMagnitude = 0;
    public Vector2 leftStickInput = Vector2.Zero;
    public Vector2 rightStickInput = Vector2.Zero;

	public int sideCheck = 0;

    private float leftPaddleVerticalVelocity = 0;
    private float rightPaddleVerticalVelocity = 0;
    

    public bool is_played = false;

    private float leftBoundX;
    private float rightBoundX;
    
    public int GameMode;

    private Vector3 originalBallScale = new Vector3(1, 1, 1);
    [Export] private float maxStretchAmount = 0.9f;
    [Export] private float stretchLerpSpeed = 2.0f;

    private float stretchTimer = 0f;
    
    [Export] public Node3D all_scene_item;
    [Export] public MeshInstance3D Background;
    [Export] public Node3D Centerline;
    [Export] public MeshInstance3D cannonball;
    [Export] public GpuParticles3D tail;
    [Export] public MeshInstance3D whiteball;





    //camera settings
    private Vector3 originalCameraRotation;
    private float breathTime = 0f;
    private float breathRotateAmplitude = 0.01f;
    private float breathFrequency = 1f;
    

    public override void _Ready()
    {
        leftPaddleX = leftPaddle.GlobalPosition.X;
        rightPaddleX = rightPaddle.GlobalPosition.X;
        parabolaA = (ballHeight - ballPeakHeight) / (leftPaddleX * leftPaddleX);
        originalBallScale = ball.Scale;
        InitMatch();
        GetMoveAreaPosition();
         originalBallScale = new Vector3(1, 1, 1);
        stretchTimer = 0f;
        GameMode = 1;
    }

    public override void _Process(double delta)
    {
        PollInput((float)delta);
        PaddleMovement((float)delta);
        BallMovement((float)delta);
        CheckPaddleCollision();
        CheckForScore();

        if (GameMode == 1)
        {
            UpdateCameraPosition((float)delta);
        }

        UpdateBallSquashAndStretch((float)delta);

        GD.Print(ball.Scale);
        
        
    }

    // Ball movement with speed adjustments
    public void BallMovement(float delta)
    {
        ball.Translate(ballVelocity * delta);
        if (GameMode == 1)
        {
            UpdateBallParabolaHeight();
        }
        else 
        {
            Vector3 pos = ball.GlobalPosition;
            pos.Y = ballHeight;
            ball.GlobalPosition = pos;
        }
        bool outOfBoundsTop = ball.Position.Z > tableSize.Y / 2.0f;
        bool outOfBoundsBottom = ball.Position.Z < -tableSize.Y / 2.0f;
        if (outOfBoundsTop && ballVelocity.Z > 0.0f || outOfBoundsBottom && ballVelocity.Z < 0.0f)
        {
            ballVelocity.Z *= -1;
            ApplyWallCollisionStretch();
        }

        UpdateBallSquashAndStretch(delta);
    }
    
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey eventKey && eventKey.Pressed && !eventKey.Echo)
        {
            if (eventKey.Keycode == Key.Space) OnSpacePressed();
        }
    }



    //switch scene
    public void OnSpacePressed()
    {
        if (GameMode == 1)
        {
            AudioServer.SetBusMute(0, true);
            all_scene_item.Visible = false;
            Background.Visible = true;
            Centerline.Visible = true;
            cannonball.Visible = false;
            whiteball.Visible = true;
            tail.Visible = false;
            L_car.Visible = false;
            R_car.Visible = false;
            LP.Visible = true;
            RP.Visible = true;
            effects.Visible = false;
            
            GameMode = 0;
        }

        else if (GameMode == 0)
        {
            AudioServer.SetBusMute(0, false);
            all_scene_item.Visible = true;
            Background.Visible = false;
            Centerline.Visible = false;
            cannonball.Visible = true;
            whiteball.Visible = false;
            tail.Visible = true;
            L_car.Visible = true;
            R_car.Visible = true;
            LP.Visible = false;
            RP.Visible = false;
            effects.Visible = true;
            
            GameMode = 1;
        }
        
    }
    public void GetMoveAreaPosition()
    {
        float moveareacenterX = cameraMoveArea.GlobalPosition.X;
        float width = ((BoxShape3D)cameraMoveArea.Shape).Size.X;
        float halfWidth = width / 2.0f;
        leftBoundX = moveareacenterX - halfWidth; 
        rightBoundX = moveareacenterX + halfWidth;
    }


    //cameras
    public void UpdateCameraPosition(float delta)
    {
        float ball_X = ball.GlobalPosition.X;
        float camera_X = camera.GlobalPosition.X;
        float camera_Rotation_Z = camera.Rotation.Z;
        float camera_Rotation_X = camera.Rotation.X;
        if (leftBoundX <= ball_X && rightBoundX >= ball_X)
        {
            if (ball_X > camera_X)
            {
                camera_X += 0.002f;
                // camera_Rotation_Y -= 0.0001f;
                // camera_Rotation_Z += 0.0001f;
            }

            if (ball_X < camera_X)
            {
                camera_X -= 0.002f;
                // camera_Rotation_Y += 0.0001f;
                // camera_Rotation_Z -= 0.0001f;
            }
            camera.GlobalPosition = new Vector3(camera_X, camera.GlobalPosition.Y, camera.GlobalPosition.Z);
            //camera.Rotation = new Vector3(camera_Rotation_X, camera.Rotation.Y, camera_Rotation_Z);

            breathTime += delta;
    

            Mathf.Sin(breathTime * breathFrequency * 2 * Mathf.Pi);
            float breathRotateZ = Mathf.Sin(breathTime * breathFrequency * 2 * Mathf.Pi) * breathRotateAmplitude;
    

            camera.Rotation = new Vector3(
                camera.Rotation.X,
                camera.Rotation.Y,
                breathRotateZ
            );
        }
        
    }

    public void UpdateBallParabolaHeight()
    {
        float currentX = ball.GlobalPosition.X;

        float newY = parabolaA * (currentX * currentX) + ballPeakHeight;

        newY = Mathf.Max(newY, ballHeight);

        Vector3 newPos = ball.GlobalPosition;
        newPos.Y = newY;
        ball.GlobalPosition = newPos;
    }

    // Paddle movement
    public void PaddleMovement(float delta)
    {
        Vector3 leftPaddlePosition = leftPaddle.Position;
        leftPaddlePosition.Z += leftStickInput.Y * paddleSpeed * leftStickMagnitude * delta;
        if(leftStickInput.Y < -0.02)
        {
            _animPlayerA.Play("goup");
        }
        else if (leftStickInput.Y > 0.02)
        {
            _animPlayerA.Play("godown");
        }
        else
        {
            _animPlayerA.Pause();
        }
        leftPaddlePosition.Z = Mathf.Clamp(leftPaddlePosition.Z, (-tableSize.Y + leftPaddle.Scale.Z) / 2, (tableSize.Y - leftPaddle.Scale.Z) / 2);
        leftPaddleVerticalVelocity = (leftPaddlePosition - leftPaddle.Position).Length();
        leftPaddle.Position = leftPaddlePosition;

        Vector3 rightPaddlePosition = rightPaddle.Position;
        rightPaddlePosition.Z += rightStickInput.Y * paddleSpeed * rightStickMagnitude * delta;
        if(rightStickInput.Y < -0.02)
        {
            _animPlayerB.Play("goup");
        }
        else if (rightStickInput.Y > 0.02)
        {
            _animPlayerB.Play("godown");
        }
        else
        {
            _animPlayerB.Pause();
        }
        rightPaddlePosition.Z = Mathf.Clamp(rightPaddlePosition.Z, (-tableSize.Y + rightPaddle.Scale.Z) / 2, (tableSize.Y - rightPaddle.Scale.Z) / 2);
        rightPaddleVerticalVelocity = (rightPaddlePosition - rightPaddle.Position).Length();
        rightPaddle.Position = rightPaddlePosition;
    }

    // Initialize match and set ball starting velocity
    public void InitMatch()
    {
        float spawnY = (GameMode == 0) ? ballHeight : ballPeakHeight;
        ball.GlobalPosition = new Vector3(3, spawnY,0);
        float angle = Mathf.DegToRad(random.Next(-30, 30));
        int horizontalDirection = -1;//random.Next(0, 2) == 0 ? 1 : -1;
        float velocityX = horizontalDirection * Mathf.Cos(angle);
        float velocityZ = Mathf.Sin(angle);
        ballVelocity = new Vector3(velocityX, 0, velocityZ) * ballSpeed;
        fire.Play();

        ResetBallScale();
        PlayRestartCameraAnimation();
    }

    // Restart match
    public void LooseMatch()
    {
        InitMatch();
    }

    // Handle joystick input for paddles (Same joystick)
    public void PollInput(float delta)
    {
        float leftX = Input.GetJoyAxis(0, JoyAxis.LeftX);
        float leftY = Input.GetJoyAxis(0, JoyAxis.LeftY);
        leftStickMagnitude = new Vector2(leftX, leftY).Length();
        leftStickInput = new Vector2(leftX, leftY);
        if (leftStickMagnitude < 0.04f) // Fuzzy joystick setting..
        {
            leftStickInput = Vector2.Zero;
        }

        float rightX = Input.GetJoyAxis(0, JoyAxis.RightX);
        float rightY = Input.GetJoyAxis(0, JoyAxis.RightY);
        rightStickMagnitude = new Vector2(rightX, rightY).Length();
        rightStickInput = new Vector2(rightX, rightY);

        if (rightStickMagnitude < 0.04f) // Fuzzy joystick setting..
        {
            rightStickInput = Vector2.Zero;
        }
    }

 // Check paddle collision with the ball
private void CheckPaddleCollision()
{
    Node3D targetPaddle = ballVelocity.X < 0 ? leftPaddle : rightPaddle;
    float paddleHalfSizeZ = PaddleCollisionWidth;
    float paddleCenterZ = targetPaddle.GlobalPosition.Z;
    float paddleMinZ = paddleCenterZ - paddleHalfSizeZ;
    float paddleMaxZ = paddleCenterZ + paddleHalfSizeZ;



    if (Mathf.Abs(ball.GlobalPosition.X - targetPaddle.GlobalPosition.X) < targetPaddle.Scale.X / 2.0f)
    {
        if (ball.GlobalPosition.Z >= paddleMinZ && ball.GlobalPosition.Z <= paddleMaxZ)
        {
            ballVelocity.X *= -1;
            if (targetPaddle == leftPaddle)
            {
                GD.Print("left paddle");
                BounceA.Play("bounce");
                bounce.Play();

            }
            else if (targetPaddle == rightPaddle)
            {
                GD.Print("right paddle");
                BounceB.Play("bounce");
                bounce.Play();

            }

            ball.Scale = originalBallScale;

            //bounce shake
            if (GameMode == 1)
            {
                Tween tween = CreateTween();
                tween.TweenProperty(camera, "h_offset", 0.1f, 0.05f);
                tween.TweenProperty(camera, "h_offset", -0.1f, 0.05f);
                tween.TweenProperty(camera, "h_offset", 0.0f, 0.05f);
            }
            
            float distanceFromCenter = ball.GlobalPosition.Z - paddleCenterZ;
            float maxAngle = 75.0f;  
            float angle = Mathf.DegToRad(maxAngle * (distanceFromCenter / paddleHalfSizeZ));

            ballVelocity.Z = Mathf.Sin(angle) * ballSpeed;
            ballVelocity = ballVelocity.Normalized() * ballSpeed;

            bool isSmash = (targetPaddle == leftPaddle && leftPaddleVerticalVelocity > 0.07f) || 
                               (targetPaddle == rightPaddle && rightPaddleVerticalVelocity > 0.07f);
            if (isSmash)
                {
                    ApplySmashExaggeration();
                }
            else
                {
                    ApplyBallCollisionSquash();
                }


			if(leftPaddleVerticalVelocity > 0.07f && targetPaddle == leftPaddle)
            {
				ballVelocity = ballVelocity * 2.0f;
			}
            if(rightPaddleVerticalVelocity > 0.07f && targetPaddle == rightPaddle)
            {
                ballVelocity = ballVelocity * 2.0f;
            }

            if (ball.GlobalPosition.X < targetPaddle.GlobalPosition.X)
            {
                ball.GlobalPosition = new Vector3(targetPaddle.GlobalPosition.X - targetPaddle.Scale.X / 2, ball.GlobalPosition.Y, ball.GlobalPosition.Z);
            }
            else
            {
                ball.GlobalPosition = new Vector3(targetPaddle.GlobalPosition.X + targetPaddle.Scale.X / 2, ball.GlobalPosition.Y, ball.GlobalPosition.Z);
            }
            UpdateBallParabolaHeight();
        }
    }
}

    // Check if the ball goes out of bounds for scoring
    private void CheckForScore()
    {
        float padding = 2f;
        if (ball.GlobalPosition.X < -tableSize.X / 2 - padding || ball.GlobalPosition.X > tableSize.X / 2 + padding)
        {
            LooseMatch();
        }
    }

    public Vector3 Getballvelocity()
    {
        return ballVelocity;
    }


    private void UpdateBallSquashAndStretch(float delta)
    {

  
        stretchTimer += delta;
    
    
        float stretchProgress = Mathf.Clamp(stretchTimer * stretchLerpSpeed,0,1);
        float currentStretch = 1 + (maxStretchAmount * stretchProgress); // 1 → 1+maxStretchAmount
    

        Vector3 newScale = new Vector3
        (
            currentStretch, 
            1 - (maxStretchAmount * stretchProgress * 0.5f),
            1 
        );
    

        ball.Scale = ball.Scale.Lerp(newScale, delta * 1f);
    }

    private void ApplyWallCollisionStretch()
    {
        Tween tween = CreateTween();
    
        tween.TweenProperty(ball, "scale", new Vector3(1.15f, 0.85f, 1.15f), 0.005f);
        tween.TweenProperty(ball, "scale", originalBallScale, 0.07f);

        stretchTimer = 0f;
    }


    private void ApplyBallCollisionSquash()
    {
        Tween tween = CreateTween();

        tween.TweenProperty(ball, "scale", originalBallScale * new Vector3(1.2f, 0.8f, 1.2f), 0.05f);

        tween.TweenProperty(ball, "scale", originalBallScale, 0.1f);
    }

    private void ApplySmashExaggeration()
    {
        Tween tween = CreateTween();

        tween.TweenProperty(ball, "scale", originalBallScale, 0.01f);

        tween.TweenProperty(ball, "scale", new Vector3(1.1f, 0.9f, 1.1f), 0.04f);
        tween.TweenProperty(ball, "scale", originalBallScale, 0.06f);
        
        stretchTimer = 0f;
    }


    private void ResetBallScale()
    {
        ball.Scale = originalBallScale;
    }


    private void PlayRestartCameraAnimation()
    {
        Tween tween = CreateTween();

        Vector3 originalCamPos = camera.GlobalPosition;
        Vector3 zoomOutPos = originalCamPos + new Vector3(0, 0.1f, 0.2f);
        tween.TweenProperty(camera, "global_position", zoomOutPos, 0.9f);
        tween.TweenProperty(camera, "global_position", originalCamPos, 0.8f);

        //tween.Parallel().TweenProperty(camera, "rotation", new Vector3(0.1f, camera.Rotation.Y, 0), 0.5f);
        //tween.Parallel().TweenProperty(camera, "rotation", originalCameraRotation, 0.8f);
    }
}

