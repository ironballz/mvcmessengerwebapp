app.messaging = (function () {
    var prolongSessionLifetimeUrl = null;

    function setProlongSessionLifetimeUrl(url) {
        prolongSessionLifetimeUrl = url;
    }
    
    var idleTime = 0;
    var idleTimeout = null;

    function handleIdleTime(timeout, callback) {
        idleTimeout = timeout;

        setInterval(function () {
            idleTime++;
            if (idleTime == idleTimeout) {
                callback();
            }
        }, 60000);
    }

    function prolongIdleTimeout() {
        idleTime = 0;

        if (prolongSessionLifetimeUrl) {
            $.get(prolongSessionLifetimeUrl, function (isProlonged) {
                //isProlonged ? console.log("Session lifetime has been prolonged.") : console.log("Session has been expired.");
            });
        }
    }

    var webSocketMessageType = {
        JOIN: 0,
        MESSAGE: 1,
        LEAVE: 2
    };

    var postedMessageParams = {
        messageId: null,
        receiverId: null,
        isDeleted: false
    };

    function setPostedMessageParams(messageId, receiverId, isDeleted) {
        postedMessageParams.messageId = messageId;
        postedMessageParams.receiverId = receiverId;
        postedMessageParams.isDeleted = isDeleted;
    }

    function getPostedMessageParams() {
        return postedMessageParams;
    }

    function updateUserStatusOnJoin(webSocket) {
        if (webSocket.readyState == WebSocket.OPEN) {
            var message = JSON.stringify({
                Type: webSocketMessageType.JOIN,
                ClientId: clientId,
                UserInfo: {
                    Id: clientId,
                    IsOnline: true,
                }
            });
            webSocket.send(message);
        }
    }

    function updateUserStatusOnLeave(webSocket) {
        if (webSocket.readyState == WebSocket.OPEN) {
            var message = JSON.stringify({
                Type: webSocketMessageType.LEAVE,
                ClientId: clientId,
                UserInfo: {
                    Id: clientId,
                    IsOnline: false,
                }
            });
            webSocket.send(message);
        }
    }

    function sendPostedMessage(webSocket, $messageInput) {
        if (webSocket.readyState == WebSocket.OPEN) {
            var message = JSON.stringify({
                Type: webSocketMessageType.MESSAGE,
                ClientId: clientId,
                PostedMessage: {
                    Id: postedMessageParams.messageId,
                    Content: $messageInput.val(),
                    ReceiverId: postedMessageParams.receiverId,
                    IsDeleted: postedMessageParams.isDeleted
                }
            });

            webSocket.send(message);

            if (postedMessageParams.isDeleted == false) {
                $messageInput.val("");
                $messageInput.focus();
            }
            postedMessageParams.messageId = null;
            postedMessageParams.isDeleted = false;
        }

        prolongIdleTimeout();
    }

    function getUserMarkup(userData, withActionControls) {
        var $userItem = $("<div></div>").addClass("row user-info").attr("data-user-id", userData.Id);

        var $info = $("<div></div>").addClass("col-md-8");
        var $status = $("<span></span>").addClass("user-status glyphicon glyphicon-cloud" + (userData.IsOnline ? " online" : " offline"));
        var $login = $("<span></span>").attr("name", "userName").append(userData.Login);
        $userItem.append($info.append($status).append($login));

        if (withActionControls) {
            $userItem.append(getUserActionControls(userData));
        }

        return $userItem;
    };

    function getUserActionControls(userData) {
        var $actions = $("<div></div>").addClass("col-md-4 user-actions text-right");

        $actions.append($("<a></a>").attr("href", "").html($("<span></span>").addClass("glyphicon glyphicon-hourglass")).click(function (e) {
            e.preventDefault();
            prolongIdleTimeout();
            showPrivateMessagingHistory(userData);
        }));
        $actions.append($("<a></a>").attr("href", "").html($("<span></span>").addClass("glyphicon glyphicon-comment")).click(function (e) {
            e.preventDefault();
            prolongIdleTimeout();
            enablePrivateMessaging(userData);
        }));

        return $actions;
    };

    var showPrivateMessagingHistory = function (userData) {
    };

    var enablePrivateMessaging = function (userData) {
    };

    function onShowPrivateMessageHistory(callback) {
        showPrivateMessagingHistory = callback;
    };

    function onEnablePrivateMessaging(callback) {
        enablePrivateMessaging = callback;
    }

    function getMessageMarkup(messageData) {
        var $messageItem;
        if (messageData.IsDeleted) {
            $messageItem = $("<div></div>").addClass("row system-message text-primary").append("Message has been deleted.");
        }
        else {
            $messageItem = $("<div></div>").addClass("row message" + (messageData.ReceiverId != null ? " message-private" : "")).attr("data-message-id", messageData.Id);

            var $header = $("<div></div>").addClass("message-info").append(messageData.RecordDate + " " + messageData.SenderName + " sent:");
            var $content = $("<div></div>").addClass("message-content").html(messageData.Content);
            var $footer = $("<div></div>").addClass("message-actions text-right");
            if (messageData.ModifiedDate) {
                $footer.append($("<span></span>").addClass("modified").append("Modified on " + messageData.ModifiedDate)).append(" ");
            }
            if (messageData.SenderId == clientId) {
                $footer.append($("<a></a>").attr("href", "#").text("Edit").click(function (e) {
                    e.preventDefault();
                    prolongIdleTimeout();
                    editPostedMessage(messageData);
                })).append(" | ");
                $footer.append($("<a></a>").attr("href", "#").text("Delete").click(function (e) {
                    e.preventDefault();
                    prolongIdleTimeout();
                    deletePostedMessage(messageData);
                }));
            }
            else {
                $footer.append("&nbsp;");
            }
            $messageItem.append($header).append($content).append($footer);
        }

        return $messageItem;
    };

    function editPostedMessage(messageData) {
    }

    function deletePostedMessage(messageData) {
    }

    function onEditPostedMessage(callback) {
        editPostedMessage = callback;
    };

    function onDeletePostedMessage(callback) {
        deletePostedMessage = callback;
    }

    return {
        setProlongSessionLifetimeUrl: setProlongSessionLifetimeUrl,
        handleIdleTime: handleIdleTime,

        webSocketMessageType: webSocketMessageType,

        setPostedMessageParams: setPostedMessageParams,
        getPostedMessageParams: getPostedMessageParams,

        updateUserStatusOnJoin: updateUserStatusOnJoin,
        updateUserStatusOnLeave: updateUserStatusOnLeave,

        sendPostedMessage: sendPostedMessage,

        getUserMarkup: getUserMarkup,
        onShowPrivateMessageHistory: onShowPrivateMessageHistory,
        onEnablePrivateMessaging: onEnablePrivateMessaging,

        getMessageMarkup: getMessageMarkup,
        onEditPostedMessage: onEditPostedMessage,
        onDeletePostedMessage: onDeletePostedMessage
    };
})();