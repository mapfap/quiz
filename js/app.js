console.log("Hello Geek.. What you are going to do?");
var app = angular.module('myApp', []);
var server = "http://localhost:10831/quiz/";

app.controller('myCtrl', function($scope, $http, $interval) {

  $scope.EMPTY = "EMPTY";
  $scope.GREETING = "GREETING";
  $scope.WELCOME_BACK = "WELCOME_BACK";
  $scope.SCOREBOARD = "SCOREBOARD";
  $scope.QUESTION = "QUESTION";
  $scope.GAMEOVER = "GAMEOVER";
  $scope.CORRECT = "CORRECT";
  $scope.INCORRECT = "INCORRECT";
  $scope.ERROR = "ERROR";

  $scope.layout = $scope.EMPTY;
  $scope.score = 0;
  $scope.consecutive = 0;
  $scope.maxConsecutive = 0;

  $scope.timerPromise;
  $scope.totalElapsedMs = 0;
    $scope.elapsedMs = 0;
    $scope.startTime;

  $http.get(server + "hello")
  .then(function(response) {
    response = JSON.parse(response.data)
    if (response.ResponseCode == $scope.GREETING) {
      $scope.timeLeft = response.TimeLeft;
      $scope.layout = $scope.GREETING;
    } else if(response.ResponseCode == $scope.WELCOME_BACK) {
      $scope.timeLeft = response.TimeLeft;
      $scope.layout = $scope.WELCOME_BACK;
    } else if(response.ResponseCode == $scope.SCOREBOARD) {
      $scope.layout = $scope.SCOREBOARD;
    }
     else {
      $scope.errorMessage = response.Message
      $scope.layout = $scope.ERROR;
    }
  }, function (response) {
    $scope.errorMessage = "Connection Error";
    $scope.layout = $scope.ERROR;
  });

  $scope.play = function() {
    $http.get(server + "ready?timeLeft=" + $scope.calculateTimeLeft())
    .then(function(response) {
      response = JSON.parse(response.data)
      if (response.ResponseCode == $scope.QUESTION) {
        $scope.question = response;
        $scope.layout = $scope.QUESTION;
        $scope.startWatch();
       } else {
        $scope.errorMessage = response.Message
        $scope.layout = $scope.ERROR;
      }
    }, function (response) {
      $scope.errorMessage = "Connection Error";
      $scope.layout = $scope.ERROR;
    });
  }

  $scope.answer = function(answer) {

    var apiRequest = server + "answer?timeLeft=" + $scope.calculateTimeLeft() + "&questionID=" + $scope.question.QuestionID + "&answer=" + answer;
    // console.log(apiRequest);


    $scope.stopWatch();
    $http.get(apiRequest)
    .then(function(response) {

      response = JSON.parse(response.data)
      // console.log(response)
      if (response.ResponseCode == $scope.CORRECT) {
        $scope.score += 1;
        $scope.consecutive += 1;
        if ($scope.consecutive > $scope.maxConsecutive) {
          $scope.maxConsecutive = $scope.consecutive;
        }
        $scope.layout = $scope.CORRECT;
      } else if(response.ResponseCode == $scope.INCORRECT) {
        $scope.consecutive = 0;
        $scope.layout = $scope.INCORRECT;
      } else if(response.ResponseCode == $scope.GAMEOVER) {

        $scope.layout = $scope.GAMEOVER;
      } else {
        $scope.errorMessage = response.Message
        $scope.layout = $scope.ERROR;
      }
    }, function (response) {
      $scope.errorMessage = "Connection Error";
      $scope.layout = $scope.ERROR;
    });
  }


  $scope.startWatch = function() {
      if (! $scope.timerPromise) {
        $scope.startTime = new Date();
        $scope.timerPromise = $interval(function() {
          var now = new Date();
          $scope.elapsedMs = now.getTime() - $scope.startTime.getTime();
        }, 1);
      }
    };

    $scope.stopWatch = function() {
      if ($scope.timerPromise) {
        $interval.cancel($scope.timerPromise);
        $scope.timerPromise = undefined;
        $scope.totalElapsedMs += $scope.elapsedMs;
        $scope.elapsedMs = 0;
      }
    };

    $scope.getElapsedMs = function() {
      return $scope.totalElapsedMs + $scope.elapsedMs;
    };

    $scope.calculateTimeLeft = function() {
      var left = $scope.timeLeft - $scope.getElapsedMs();
      if (left <= 0) {
        return 0;
      }
      return left;
    };

    $scope.isHUDActive = function() {
      return $scope.layout == $scope.QUESTION || $scope.layout == $scope.CORRECT || $scope.layout == $scope.INCORRECT;
    }

    $scope.getHUDTimeLeft = function() {
      if ($scope.isHUDActive()) {
        return $scope.calculateTimeLeft();
      }
      return "---.------";
    };

    $scope.getHUDScore = function() {
      if ($scope.isHUDActive()) {
        return $scope.score;
      }
      return "-";
    };

    $scope.getHUDConScore = function() {
      if ($scope.isHUDActive()) {
        return $scope.maxConsecutive;
      }
      return "-";
    };

    $scope.getHUDPlayer = function() {
      return "SWONGT2";
    };

});
