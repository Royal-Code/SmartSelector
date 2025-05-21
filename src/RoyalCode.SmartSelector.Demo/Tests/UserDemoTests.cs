using RoyalCode.SmartSelector.Demo.Details;
using RoyalCode.SmartSelector.Demo.Entities;
using RoyalCode.SmartSelector.Demo.Infra;

namespace RoyalCode.SmartSelector.Demo.Tests;

public class UserDemoTests
{

    [Fact]
    public void GetSelectExpression()
    {
        // act
        var expression = UserDetails.SelectUserExpression;
        // assert
        Assert.NotNull(expression);
    }

    [Fact]
    public void Create_UserDetails_From_User()
    {
        // arrange
        var user = new User("John Doe");

        // act
        var details = UserDetails.From(user);

        // assert
        Assert.NotNull(details);
        Assert.Equal(user.Id, details.Id);
        Assert.Equal(user.Name, details.Name);
        Assert.Equal(user.Status, (UserStatus)details.Status);
    }

    [Fact]
    public void Create_UserDetails_Using_ToUserDetails()
    {
        // arrange
        var user = new User("John Doe");

        // act
        var details = user.ToUserDetails();

        // assert
        Assert.NotNull(details);
        Assert.Equal(user.Id, details.Id);
        Assert.Equal(user.Name, details.Name);
        Assert.Equal(user.Status, (UserStatus)details.Status);
    }

    [Fact]
    public void Query_UserDetails_From_Database()
    {
        // arrange
        var db = new AppDbContext();
        db.Database.EnsureCreated();
        db.Users.Add(new User("John Doe"));
        db.Users.Add(new User("Jane Doe"));
        db.Users.Add(new User("Jack Doe"));
        db.SaveChanges();
        db.ChangeTracker.Clear();

        // act
        var users = db.Users.ToList();

        // assert
        Assert.NotNull(users);
        Assert.Equal(3, users.Count);
        Assert.Contains(users, u => u.Name == "John Doe");
        Assert.Contains(users, u => u.Name == "Jane Doe");
        Assert.Contains(users, u => u.Name == "Jack Doe");
    }

    [Fact]
    public void Select_UserDetails_From_List()
    {
        // arrange
        var user = new List<User>
        {
            new User("John Doe"),
            new User("Jane Doe"),
            new User("Jack Doe")
        };

        // act
        var details = user.SelectUserDetails().ToList();

        // assert
        Assert.NotNull(details);
        Assert.Equal(3, details.Count);
        Assert.Contains(details, d => d.Name == "John Doe");
        Assert.Contains(details, d => d.Name == "Jane Doe");
        Assert.Contains(details, d => d.Name == "Jack Doe");
    }
}
