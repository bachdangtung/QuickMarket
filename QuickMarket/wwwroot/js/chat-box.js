// Chat Box Functionality
$(document).ready(function() {
    // Chat box toggle
    $("#chat-box-toggle").click(function() {
        $("#chat-box-container").fadeToggle(300);
        loadChatList();
    });

    // Chat box close
    $("#chat-box-close").click(function() {
        $("#chat-box-container").fadeOut(300);
    });

    // Chat box minimize
    $("#chat-box-minimize").click(function() {
        const chatBody = $("#chat-box-body");
        if (chatBody.is(":visible")) {
            chatBody.slideUp(300);
            $(this).html('<i class="bi bi-plus-lg"></i>');
        } else {
            chatBody.slideDown(300);
            $(this).html('<i class="bi bi-dash-lg"></i>');
        }
    });

    // Load chat list function
    function loadChatList() {
        $.ajax({
            url: '/Message/Index',
            type: 'GET',
            success: function(response) {
                if (Array.isArray(response)) {
                    // It's the JSON response with all chats
                    renderChatList(response);
                } else {
                    // It's HTML, we need to make another call
                    $.ajax({
                        url: '/Message/GetAllChats',
                        type: 'GET',
                        dataType: 'json',
                        success: function(chats) {
                            renderChatList(chats);
                        },
                        error: function() {
                            $("#chat-box-list").html('<div class="p-3 text-center text-muted">Không thể tải tin nhắn</div>');
                        }
                    });
                }
            },
            error: function() {
                $("#chat-box-list").html('<div class="p-3 text-center text-muted">Không thể tải tin nhắn</div>');
            }
        });
    }

    // Render chat list
    function renderChatList(chats) {
        if (!chats || chats.length === 0) {
            $("#chat-box-list").html('<div class="p-3 text-center text-muted">Không có tin nhắn</div>');
            return;
        }

        let html = '';
        // Sort chats by latest message time
        chats.sort((a, b) => {
            const aTime = a.messages && a.messages.length > 0 ? 
                new Date(a.messages[a.messages.length - 1].sentTime) : new Date(0);
            const bTime = b.messages && b.messages.length > 0 ? 
                new Date(b.messages[b.messages.length - 1].sentTime) : new Date(0);
            return bTime - aTime; // Descending order (newest first)
        });

        // Take only the 5 most recent chats
        const recentChats = chats.slice(0, 5);

        // Build HTML for each chat
        recentChats.forEach(chat => {
            const lastMessage = chat.messages && chat.messages.length > 0 ? 
                chat.messages[chat.messages.length - 1] : null;
            const messagePreview = lastMessage ? 
                (lastMessage.messageText.length > 25 ? 
                    lastMessage.messageText.substring(0, 25) + '...' : lastMessage.messageText) : 
                'Chưa có tin nhắn';
            const time = lastMessage ? 
                formatMessageTime(new Date(lastMessage.sentTime)) : '';
            
            html += `
            <div class="chat-list-item" onclick="window.location='/Message/Index?userId=${chat.otherUserId}'">
                <div class="d-flex justify-content-between align-items-start">
                    <h6 class="mb-1">${chat.otherUsername}</h6>
                    <small class="time">${time}</small>
                </div>
                <p>${messagePreview}</p>
            </div>`;
        });

        $("#chat-box-list").html(html);
    }

    // Format time for display
    function formatMessageTime(date) {
        const now = new Date();
        const diffDays = Math.floor((now - date) / (1000 * 60 * 60 * 24));
        
        if (diffDays === 0) {
            // Today, show time
            return date.toLocaleTimeString([], {hour: '2-digit', minute: '2-digit'});
        } else if (diffDays === 1) {
            // Yesterday
            return 'Hôm qua';
        } else if (diffDays < 7) {
            // Within a week
            const days = ['CN', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7'];
            return days[date.getDay()];
        } else {
            // Older than a week
            return date.toLocaleDateString();
        }
    }

    // Initialize SignalR connection for real-time updates
    // Only if the user is authenticated
    if ($("#chat-box-toggle").length) {
        const connection = new signalR.HubConnectionBuilder()
            .withUrl("/chatHub")
            .build();

        connection.start()
            .then(() => console.log("Connected to chat hub"))
            .catch(err => console.error("SignalR Connection Error: ", err));

        // Handle received messages to update UI
        connection.on("ReceiveMessage", (senderId, message, timestamp) => {
            // Update notification badge
            const badge = $("#chat-box-toggle .chat-badge");
            if (badge.length) {
                const count = parseInt(badge.text()) + 1;
                badge.text(count);
            } else {
                $("#chat-box-toggle").append('<span class="chat-badge">1</span>');
            }
            
            // If chat box is open, refresh the list
            if ($("#chat-box-container").is(":visible")) {
                loadChatList();
            }
        });
    }
});
