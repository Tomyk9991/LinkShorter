﻿using System;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Npgsql;

namespace LinkShorter.Controllers
{
    [Controller]
    [Route("/api/login")]
    public class LoginController : ControllerBase
    {
        private DatabaseWrapper _databaseWrapper;
        private PasswordManager _passwordManager;
        private readonly StringGenerator _stringGenerator;
        private readonly SessionManager _sessionManager;

        public LoginController(DatabaseWrapper databaseWrapper, PasswordManager passwordManager,
            StringGenerator stringGenerator, SessionManager sessionManager)
        {
            this._databaseWrapper = databaseWrapper;
            this._passwordManager = passwordManager;
            this._stringGenerator = stringGenerator;
            this._sessionManager = sessionManager;
        }


        [Route("login")]
        [HttpPost]
        public string Login([FromBody] LoginData loginData)
        {
            /*Console.WriteLine("username: " + loginData.Username);
            Console.WriteLine("password: " + loginData.Password);*/
            return "ok";
        }

        [Route("register")]
        [HttpPost]
        public string Register([FromBody] LoginData loginData)
        {
            if (CheckIfDuplicateUsernameExists(loginData.Username)) return "username is already in use";

            var salt = _passwordManager.SaltGenerator();

            var hash = _passwordManager.Hash(loginData.Password, salt);


            string apikey;
            while (true)
            {
                apikey = _stringGenerator.GenerateApiKey();
                if (!CheckIfDuplicateApikeyExists(apikey)) break;
            }


            var insert =
                @$"INSERT INTO users(id, username, password, salt, apikey) VALUES (DEFAULT,'{loginData.Username}', '{hash}', '{salt}', '{apikey}');
SELECT id FROM users WHERE username = '{loginData.Username}';";
            var insertion = new NpgsqlCommand(insert, _databaseWrapper.GetDatabaseConnection());
            var result = insertion.ExecuteScalar();


            var resp = new HttpResponseMessage();

            Console.WriteLine("userid: " + result.ToString());

            Response.Cookies.Append("session", _sessionManager.Register(result.ToString()));
            return "ok";
        }


        private bool CheckIfDuplicateUsernameExists(string username)
        {
            var checkDuplicates = @$"SELECT username FROM users WHERE username = '{username}' LIMIT 1;";
            var cmdCheckDuplicates = new NpgsqlCommand(checkDuplicates, _databaseWrapper.GetDatabaseConnection());

            var duplicates = cmdCheckDuplicates.ExecuteScalar();

            return duplicates != null;
        }

        private bool CheckIfDuplicateApikeyExists(string apikey)
        {
            var checkDuplicates = @$"SELECT apikey FROM users WHERE apikey = '{apikey}' LIMIT 1;";
            var cmdCheckDuplicates = new NpgsqlCommand(checkDuplicates, _databaseWrapper.GetDatabaseConnection());

            var duplicates = cmdCheckDuplicates.ExecuteScalar();

            return duplicates != null;
        }
    }
}