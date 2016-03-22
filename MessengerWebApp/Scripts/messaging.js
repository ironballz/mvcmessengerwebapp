// Messaging module.
app.messaging = (function () {
    // Private prolong session lifetime action url.
    var prolongSessionLifetimeUrl = null;

    // Public.
    // Sets prolong session lifetime action url.
    function setProlongSessionLifetimeUrl(url) {
        prolongSessionLifetimeUrl = url;
    }
    
    // Private idle time counter.
    var idleTime = 0;
    // Private idle timeout.
    var idleTimeout = null;

    // Public.
    // Handles user idle time.
    function handleIdleTime(timeout, callback) {
        idleTimeout = timeout;

        // Updates idle time counter each minute and check if
        // idle timeout is reached. Invokes callback when timeout has been reached.
        setInterval(function () {
            idleTime++;
            if (idleTime == idleTimeout) {
                callback();
            }
        }, 60000);
    }

    // Private.
    // Prolongs user idle timeout when user performes any action.
    function prolongIdleTimeout() {
        idleTime = 0;

        if (prolongSessionLifetimeUrl) {
            // Sends AJAX request to prolong current session timeout 
            // due to web socket handshaking does not check session state
            // and web socket continue to send and receive message 
            // even after user session has been expired.
            $.get(prolongSessionLifetimeUrl, function (isProlonged) {
                //isProlonged ? console.log("Session lifetime has been prolonged.") : console.log("Session has been expired.");
            });
        }
    }

    // Public web socket message type enum.
    var webSocketMessageType = {
        JOIN: 0,
        MESSAGE: 1,
        LEAVE: 2
    };

    // Posted message parameters for private messaging,
    // editing and deleting interaction.
    var postedMessageParams = {
        messageId: null,
        receiverId: null,
        isDeleted: false
    };

    // Public.
    // Sets posted message parameters.
    function setPostedMessageParams(messageId, receiverId, isDeleted) {
        postedMessageParams.messageId = messageId;
        postedMessageParams.receiverId = receiverId;
        postedMessageParams.isDeleted = isDeleted;
    }

    // Public.
    // Gets posted message parameters.
    function getPostedMessageParams() {
        return postedMessageParams;
    }

    // Public.
    // Updates user status on joining the chat for all 
    // online users via web socket connection.
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

    // Public.
    // Updates user status on leaving the chat for all
    // online users via web socket connection.
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

    // Public.
    // Sends posted message text via web socket connection.
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

    // Public.
    // Gets user item markup jQuery object according to user data parameter.
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

    // Private.
    // Gets user item private messaging link controls.
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

    // Private.
    // This function called on private message history link clicked.
    var showPrivateMessagingHistory = function (userData) {
    };

    // Private.
    // This function called on private messaging link clicked.
    var enablePrivateMessaging = function (userData) {
    };

    // Public.
    // Sets private message history function on link clicked.
    function onShowPrivateMessageHistory(callback) {
        showPrivateMessagingHistory = callback;
    };

    // Public.
    // Sets enable private messaging function on link clicked.
    function onEnablePrivateMessaging(callback) {
        enablePrivateMessaging = callback;
    }

    // Public.
    // Gets message item markup jQuery object according to
    // message data parameter.
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

    // Private.
    // This function called on edit posted message link clicked.
    function editPostedMessage(messageData) {
    }

    // Private.
    // This function called on delete posted message link clicked.
    function deletePostedMessage(messageData) {
    }

    // Public.
    // Sets edit posted message function on link clicked.
    function onEditPostedMessage(callback) {
        editPostedMessage = callback;
    };

    // Public.
    // Sets delete posted message function on link clicked.
    function onDeletePostedMessage(callback) {
        deletePostedMessage = callback;
    }

    // Exposing public functions.
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