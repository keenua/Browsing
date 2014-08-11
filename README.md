Browsing
========

A wrapper for C#'s WebClient

Uses [HtmlAgilityPack library](http://htmlagilitypack.codeplex.com/) to extend the functionality of the standard .Net WebClient.
An useful tool to create web crawlers and parsers.

An example of logging in to GitHub

```C#
Browser browser = new Browser();
browser.Encoding = Encoding.UTF8;

// Navigate to the login page
var loginPage = browser.Navigate("https://github.com/login");

// Extract arguments from the login form using xpath
var loginArgs = browser.ExtractArgs(loginPage, @"//div[@id='login']/form");

// Set arguments
loginArgs["login"].SetValue("your_login");
loginArgs["password"].SetValue("your_password");

// Post those arguments
var resultPage = browser.Post("https://github.com/session", loginArgs, false, true);

// Save the page to file
resultPage.Save("result.html");
```
