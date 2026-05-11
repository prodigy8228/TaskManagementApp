const admin = require('firebase-admin');

// Initialize with service account from environment variable
const serviceAccount = JSON.parse(process.env.FIREBASE_SERVICE_ACCOUNT);
admin.initializeApp({
  credential: admin.credential.cert(serviceAccount)
});

async function sendOverdueNotifications() {
  const db = admin.firestore();
  const now = new Date().toISOString();

  console.log("Checking for overdue tasks...");

  // 1. Find tasks that are overdue and not completed
  const tasksSnapshot = await db.collection('TaskRecord')
    .where('task_due_date', '<', now)
    .where('is_completed', '==', false)
    .get();

  if (tasksSnapshot.empty) {
    console.log("No overdue tasks found today.");
    return;
  }

  // 2. Map Task to UserId (using a Map to handle multiple tasks for one user)
  const userTasks = new Map();
  tasksSnapshot.forEach(doc => {
    const task = doc.data();
    if (task.userId) { // Ensure your TaskRecord has a userId field
      if (!userTasks.has(task.userId)) {
        userTasks.set(task.userId, []);
      }
      userTasks.get(task.userId).push(task.title);
    }
  });

  // 3. Fetch tokens from 'users' collection for these specific users
  const messages = [];
  const userIds = Array.from(userTasks.keys());
  
  // Firestore 'in' queries are limited to 30 items at a time
  // For simplicity, we fetch each user or use a loop
  for (const userId of userIds) {
    const userDoc = await db.collection('User').doc(userId).get();
    
    if (userDoc.exists) {
      const userData = userDoc.data();
      const token = userData?.fcmToken; // Optional chaining prevents 'undefined' errors
      if (token && typeof token === 'string' && token.trim() !== ') {
        const pendingTasks = userTasks.get(userId);
        messages.push({
          token: token,
          notification: {
            title: "Task Overdue!",
            body: pendingTasks.length === 1 
                  ? `Your task "${pendingTasks[0]}" is past its deadline.`
                  : `You have ${pendingTasks.length} overdue tasks waiting.`
          }
        });
      }
    }
  }

  // 4. Send batch notifications
  if (messages.length > 0) {
    const response = await admin.messaging().sendEach(messages);
    console.log(`Successfully sent ${response.successCount} notifications.`);
  } else {
    console.log("No valid FCM tokens found for overdue tasks.");
  }
}

sendOverdueNotifications().catch(console.error);
