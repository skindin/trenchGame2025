
var socket;

mergeInto(LibraryManager.library, {

Connect: function WebSocketConnect(urlPtr) {
    var url = UTF8ToString(urlPtr);
    // console.log('recieved url "',url,'" from client');
    socket = new WebSocket(url);

    socket.onopen = function() {
        if (typeof WebGLSocket_OnOpen !== 'undefined')
        {
            console.log('WebSocket is open now.');
            WebGLSocket_OnOpen();
        }
    };

    socket.onmessage = function(event) {
        if (typeof WebGLSocket_OnMessage !== 'undefined')
            {
                console.log('WebSocket recieved data');
                WebGLSocket_OnMessage(event.data);
            }
        // console.log('WebSocket message received:', event.data);
        // SendMessage('GameClient', 'OnWebSocketMessage', event.data);
    };

    socket.onclose = function() {
        // console.log('WebSocket is closed now.');
        if (typeof WebGLSocket_OnClose !== 'undefined')
            {
                console.log('WebSocket is closed now.');
                WebGLSocket_OnClose();
            }
        // SendMessage('GameClient', 'OnWebSocketClose');
    };

    socket.onerror = function(error) {
        console.log('WebSocket error:', error);
        if (typeof WebGLSocket_OnError !== 'undefined')
            {
                console.log('WebSocket error:', error);
                WebGLSocket_OnError(error.message);
            }
        // SendMessage('GameClient', 'OnWebSocketError', error.message);
    };
},

Send: function WebSocketSend(message) {
    if (socket && socket.readyState === WebSocket.OPEN) {
        socket.send(message);
    }
},

Close: function WebSocketClose() {
    if (socket) {
        socket.close();
    }
}

});
