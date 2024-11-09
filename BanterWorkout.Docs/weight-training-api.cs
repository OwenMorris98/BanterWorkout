// Domain Models
public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string Username { get; set; }
    public List<Workout> Workouts { get; set; }
    public List<Group> Groups { get; set; }
}

public class Workout
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public DateTime Date { get; set; }
    public Guid UserId { get; set; }
    public List<Exercise> Exercises { get; set; }
    public bool IsShared { get; set; }
}

public class Exercise
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public int Sets { get; set; }
    public int Reps { get; set; }
    public decimal Weight { get; set; }
    public Guid WorkoutId { get; set; }
}

public class Group
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public Guid CreatorId { get; set; }
    public List<User> Members { get; set; }
    public List<WorkoutShare> SharedWorkouts { get; set; }
}

public class WorkoutShare
{
    public Guid Id { get; set; }
    public Guid WorkoutId { get; set; }
    public Guid GroupId { get; set; }
    public DateTime SharedDate { get; set; }
}

// Feature Example: Creating a Workout (Vertical Slice)
namespace Features.Workouts.Create
{
    public record CreateWorkoutCommand
    {
        public string Name { get; init; }
        public List<ExerciseDto> Exercises { get; init; }
    }

    public record ExerciseDto
    {
        public string Name { get; init; }
        public int Sets { get; init; }
        public int Reps { get; init; }
        public decimal Weight { get; init; }
    }

    public class CreateWorkoutEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder builder)
        {
            builder.MapPost("/api/workouts", async (
                CreateWorkoutCommand command,
                ISender sender) =>
                await sender.Send(command));
        }
    }

    public class CreateWorkoutHandler : IRequestHandler<CreateWorkoutCommand, Guid>
    {
        private readonly DbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public async Task<Guid> Handle(CreateWorkoutCommand command, CancellationToken ct)
        {
            var workout = new Workout
            {
                Id = Guid.NewGuid(),
                Name = command.Name,
                Date = DateTime.UtcNow,
                UserId = _currentUserService.UserId,
                Exercises = command.Exercises.Select(e => new Exercise
                {
                    Name = e.Name,
                    Sets = e.Sets,
                    Reps = e.Reps,
                    Weight = e.Weight
                }).ToList()
            };

            _context.Workouts.Add(workout);
            await _context.SaveChangesAsync(ct);

            return workout.Id;
        }
    }
}
