using System.Net;
using System.Text.Json;
using RestSharp;
using RestSharp.Authenticators;
using MovieCatalogExam.Models;

namespace MovieCatalogExam;

public class Tests
{
    private RestClient _client;
    private static string movieId;
    
    private const string BaseUrl = "http://144.91.123.158:5000/api";
    private const string Email = "rosen@softuni.test";
    private const string Password = "123456";
    
    [OneTimeSetUp]
    public void Setup()
    {
        string jwtToken = GetJwtToken(Email, Password);

        RestClientOptions options = new RestClientOptions(BaseUrl)
        {
            Authenticator = new JwtAuthenticator(jwtToken)
        };
        
        _client = new RestClient(options);
    }

    private static string GetJwtToken(string email, string password)
    {
        RestClient tempClient = new RestClient(BaseUrl);
        RestRequest request = new RestRequest("/User/Authentication", Method.Post);
        request.AddJsonBody(new {  email, password });
        
        RestResponse response = tempClient.Execute(request);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            JsonElement content = JsonSerializer.Deserialize<JsonElement>(response.Content!);
            string? token = content.GetProperty("accessToken").GetString();

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException("Invalid token");
            }
            
            return token;
        }
        else
        {
            throw new InvalidOperationException("Failed to authenticate");
        }
    }

    [Order(1)]
    [Test]
    public void CreateMovie_WithRequiredFields_ShouldSucceed()
    {
        //Arrange
        string title = "My First Movie";
        string description = "My First Movie description";
        
        RestRequest request = new RestRequest("/Movie/Create", Method.Post);
        request.AddJsonBody(new {title, description});
        
        //Act
        RestResponse response = _client.Execute(request);
        ApiResponseDto responseDto = JsonSerializer.Deserialize<ApiResponseDto>(response.Content);

        //Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(responseDto?.Movie, Is.Not.Null);
        Assert.That(responseDto.Movie.Id, Is.Not.Null.Or.Empty);
        Assert.That(responseDto.Msg, Is.EqualTo("Movie created successfully!"));
        
        movieId = responseDto.Movie.Id;
    }

    [Order(2)]
    [Test]
    public void EditMovie_ShouldSucceed()
    {
        //Arrange
        string title = "Edited title";
        string description = "Edited description";
        
        RestRequest request = new RestRequest("/Movie/Edit", Method.Put);
        request.AddQueryParameter("movieId", movieId);
        request.AddJsonBody(new {title, description});
        
        //Act
        RestResponse response = _client.Execute(request);
        ApiResponseDto responseDto = JsonSerializer.Deserialize<ApiResponseDto>(response.Content);
        
        //Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(responseDto?.Msg, Is.EqualTo("Movie edited successfully!"));
    }
    
    [Order(3)]
    [Test]
    public void GetAllMovies_ShouldSucceed()
    {
        //Arrange
        RestRequest request = new RestRequest("/Catalog/All");
        
        //Act
        RestResponse response = _client.Execute(request);
        List<ApiResponseDto> responseDto = JsonSerializer.Deserialize<List<ApiResponseDto>>(response.Content);
        
        //Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(responseDto, Is.Not.Null.Or.Empty);
    }
    
    [Order(4)]
    [Test]
    public void DeleteMovie_ShouldSucceed()
    {
        //Arrange
        RestRequest request = new RestRequest("/Movie/Delete", Method.Delete);
        request.AddQueryParameter("movieId", movieId);
        
        //Act
        RestResponse response = _client.Execute(request);
        ApiResponseDto responseDto = JsonSerializer.Deserialize<ApiResponseDto>(response.Content);
        
        //Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(responseDto?.Msg, Is.EqualTo("Movie deleted successfully!"));
    }

    [Order(5)]
    [Test]
    public void CreatMovie_WithoutRequiredFields_ShouldSFail()
    {
        //Arrange
        string title = "";
        string description = "";
        
        RestRequest request = new RestRequest("/Movie/Create", Method.Post);
        request.AddJsonBody(new {title, description});
        
        //Act
        RestResponse response = _client.Execute(request);

        //Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Order(6)]
    [Test]
    public void EditNonExistingMovie_ShouldSFail()
    {
        //Arrange
        string title = "Edited title";
        string description = "Edited description";
        string nonExistingMovieId = "nonExistingMovieId";
        
        RestRequest request = new RestRequest("/Movie/Edit", Method.Put);
        request.AddQueryParameter("movieId", nonExistingMovieId);
        request.AddJsonBody(new {title, description});
        
        //Act
        RestResponse response = _client.Execute(request);
        ApiResponseDto responseDto = JsonSerializer.Deserialize<ApiResponseDto>(response.Content);
        
        //Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(responseDto?.Msg, Is.EqualTo("Unable to edit the movie! Check the movieId parameter or user verification!"));
    }

    [Order(7)]
    [Test]
    public void DeleteNonExistingMovie_ShouldSFail()
    {
        //Arrange
        string nonExistingMovieId = "nonExistingMovieId";
        
        RestRequest request = new RestRequest("/Movie/Delete", Method.Delete);
        request.AddQueryParameter("movieId", nonExistingMovieId);
        
        //Act
        RestResponse response = _client.Execute(request);
        ApiResponseDto responseDto = JsonSerializer.Deserialize<ApiResponseDto>(response.Content);
        
        //Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(responseDto?.Msg, Is.EqualTo("Unable to delete the movie! Check the movieId parameter or user verification!"));
    }
    
    [OneTimeTearDown]
    public void TearDown()
    {
        _client.Dispose();
    }
}