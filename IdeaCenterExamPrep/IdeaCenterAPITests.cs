using System;
using System.Net;
using System.Text.Json;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using IdeaCenterExamPrep.Models;

namespace IdeaCenterExamPrep
{
    [TestFixture]
    public class IdeaCenterApiTests
    {
        private RestClient client;
        private static string lastCreatedIdeaId;
        private const string BaseURL = "http://softuni-qa-loadbalancer-2137572849.eu-north-1.elb.amazonaws.com:84";
        private const string staticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiI0ZWU1MGJmZC1kYzI4LTQ5MzMtOGYzNS0xMjU2MGE3NjE4ZDIiLCJpYXQiOiIwOC8xMi8yMDI1IDE5OjI1OjU4IiwiVXNlcklkIjoiY2FiMGM0MmYtNjNiYi00ODZiLWQyOGYtMDhkZGQ0ZTA4YmQ4IiwiRW1haWwiOiJwcmVwYXJhdGlvbkBleGFtbmFtZS5jb20iLCJVc2VyTmFtZSI6ImV4YW1wcmVwTmFtZSIsImV4cCI6MTc1NTA0ODM1OCwiaXNzIjoiSWRlYUNlbnRlcl9BcHBfU29mdFVuaSIsImF1ZCI6IklkZWFDZW50ZXJfV2ViQVBJX1NvZnRVbmkifQ.y6y6ehrw3cXzrfRB9OLk7lPA0VERrsrop3yd8D9s3hY";
        private const string loginEmail = "preparation@examname.com";
        private const string loginPassword = "exampassword";

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken;

            if (!string.IsNullOrWhiteSpace(staticToken))
            {
                jwtToken = staticToken;
            }
            else
            {
                jwtToken = GetJwtToken(loginEmail, loginPassword);
            }

            var options = new RestClientOptions(BaseURL)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };

            this.client = new RestClient(options);
        }
        private string GetJwtToken(string email,string password)
        {
            var tempClient = new RestClient(BaseURL);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { email, password });
            var response = tempClient.Execute(request);

            if(response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Failed to retrieve JWT token from the response");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code: {response.StatusCode}, Content: {response.Content}");
            }
        }
        //All tests here
        [Order(1)]
        [Test]
        public void CreateIdea_WithRequiredFields_ShouldReturnSuccess()
        {
            var ideaRequest = new IdeaDTO
            {
                Title = "new Idea",
                Description = "This is an example of creating a idea's description",
                Url = ""
            };

            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(ideaRequest);
            var response = this.client.Execute(request);
            var CreateResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(CreateResponse.Msg, Is.EqualTo("Successfully created!"));
        }

        [Order(2)]
        [Test]
        public void List_All_Ideas()
        {
            var request = new RestRequest("/api/Idea/All", Method.Get);
            var response = this.client.Execute(request);
            var createResponse = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(createResponse, Is.Not.Empty);
            Assert.That(createResponse, Is.Not.Null);
            lastCreatedIdeaId = createResponse.LastOrDefault()?.ID;
        }

        [Order(3)]
        [Test]
        public void EditLastCreatedIdea_ShouldReturnSuccess()
        {
            var editRequest = new IdeaDTO
            {
                Title = "Edited Idea",
                Description = "This is an updated test idea description.",
                Url = ""
            };

            var request = new RestRequest($"/api/Idea/Edit", Method.Put);
            request.AddQueryParameter("ideaId", lastCreatedIdeaId);
            request.AddJsonBody(editRequest);
            var response = this.client.Execute(request);
            var editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(editResponse.Msg, Is.EqualTo("Edited successfully"));
        }

        [Order(4)]
        [Test]
        public void DeleteIdeaByQueryParam_ShouldBeSuccessfull()
        {
            var request = new RestRequest("/api/Idea/Delete", Method.Delete);
            request.AddQueryParameter("ideaId", lastCreatedIdeaId);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("The idea is deleted!"));
        }

        [Order(5)]
        [Test]
        public void CreateIdeaWithInvalidData_ShouldReturnError()
        {
            {
                var ideaRequest = new IdeaDTO
                {
                    Url = ""
                };

                var request = new RestRequest("/api/Idea/Create", Method.Post);
                request.AddJsonBody(ideaRequest);
                var response = this.client.Execute(request);

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 BadRequest.");
            }
        }

        [Order(6)]
        [Test]
        public void EditNonExistingIdea_ShouldReturnError()
            {
            string nonExistingIdeaId = "2436";
                var editRequest = new IdeaDTO
                {
                    Title = "Edited Idea",
                    Description = "This is an updated test idea description.",
                    Url = ""
                };

                var request = new RestRequest($"/api/Idea/Edit", Method.Put);
                request.AddQueryParameter("ideaId", nonExistingIdeaId);
                request.AddJsonBody(editRequest);
                var response = this.client.Execute(request);

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest),"Expected status code 400 BadRequest.");
                Assert.That(response.Content, Does.Contain("There is no such idea!"));
            }

        [Order(7)]
        [Test]
        public void DeleteNonExistingIdea_ShouldReturnError()
        {
            var request = new RestRequest("/api/Idea/Delete", Method.Delete);
            request.AddQueryParameter("ideaId", lastCreatedIdeaId);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 BadRequest.");
            Assert.That(response.Content, Does.Contain("There is no such idea!"));
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            this.client?.Dispose();
        }
    }
}