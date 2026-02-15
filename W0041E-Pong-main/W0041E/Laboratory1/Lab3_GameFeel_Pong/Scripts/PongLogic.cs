using Godot;
using System;

public partial class PongLogic : Node
{
    [Export]
    public Camera3D camera;

    [Export]
    public Node3D leftPaddle;

    [Export]
    public Node3D rightPaddle;
    
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

    public override void _Ready()
    {
        leftPaddleX = leftPaddle.GlobalPosition.X;
        rightPaddleX = rightPaddle.GlobalPosition.X;
        parabolaA = (ballHeight - ballPeakHeight) / (leftPaddleX * leftPaddleX);
        InitMatch();
    }

    public override void _Process(double delta)
    {
        PollInput((float)delta);
        PaddleMovement((float)delta);
        BallMovement((float)delta);
        CheckPaddleCollision();
        CheckForScore();

    }

    // Ball movement with speed adjustments
    public void BallMovement(float delta)
    {
        ball.Translate(ballVelocity * delta);
        UpdateBallParabolaHeight();
        bool outOfBoundsTop = ball.Position.Z > tableSize.Y / 2.0f;
        bool outOfBoundsBottom = ball.Position.Z < -tableSize.Y / 2.0f;
        if (outOfBoundsTop && ballVelocity.Z > 0.0f || outOfBoundsBottom && ballVelocity.Z < 0.0f)
        {
            ballVelocity.Z *= -1;
            // Tween tween = CreateTween();
            // tween.TweenProperty(camera, "z_offset", 0.1f, 0.05f);
            // tween.TweenProperty(camera, "z_offset", -0.1f, 0.05f);
            // tween.TweenProperty(camera, "z_offset", 0.0f, 0.05f);
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
        ball.GlobalPosition = new Vector3(3, ballPeakHeight,0);
        float angle = Mathf.DegToRad(random.Next(-30, 30));
        int horizontalDirection = -1;//random.Next(0, 2) == 0 ? 1 : -1;
        float velocityX = horizontalDirection * Mathf.Cos(angle);
        float velocityZ = Mathf.Sin(angle);
        ballVelocity = new Vector3(velocityX, 0, velocityZ) * ballSpeed;
        fire.Play();
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
            Tween tween = CreateTween();
            tween.TweenProperty(camera, "h_offset", 0.1f, 0.05f);
            tween.TweenProperty(camera, "h_offset", -0.1f, 0.05f);
            tween.TweenProperty(camera, "h_offset", 0.0f, 0.05f);
            float distanceFromCenter = ball.GlobalPosition.Z - paddleCenterZ;
            float maxAngle = 75.0f;  
            float angle = Mathf.DegToRad(maxAngle * (distanceFromCenter / paddleHalfSizeZ));

            ballVelocity.Z = Mathf.Sin(angle) * ballSpeed;
            ballVelocity = ballVelocity.Normalized() * ballSpeed;


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

}
