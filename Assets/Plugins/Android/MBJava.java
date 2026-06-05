package com.mb28.mbjava;
import android.view.RoundedCorner;
import android.view.WindowInsets;
import android.view.WindowManager;
import android.app.Activity;
import android.app.AlertDialog;
import android.app.AlertDialog.Builder;
import android.content.Context;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.net.Uri;
import android.os.Build;
import android.provider.Settings;

public class MBJava {

    public static int cornerRadius(Activity activity) {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
            WindowInsets insets = activity.getWindow().getDecorView().getRootWindowInsets();
            if (insets != null) {
                RoundedCorner corner = insets.getRoundedCorner(RoundedCorner.POSITION_TOP_LEFT);
                if (corner != null) {
                    return corner.getRadius();
                }
            }
        }
        return 0;
    }

    public static void ChangeBrightness(Activity activity, float brightness) {
        WindowManager.LayoutParams layoutParams = activity.getWindow().getAttributes();
        layoutParams.screenBrightness = brightness;
        activity.getWindow().setAttributes(layoutParams);
    } 

    public static String GetDeviceName(Activity activity) {
        return Settings.Global.getString(activity.getContentResolver(), Settings.Global.DEVICE_NAME);
    }

    public static boolean isAppInstalled(Context context, String packageName) {
        PackageManager pm = context.getPackageManager();
        try {
            pm.getPackageInfo(packageName, 0);
            return true;
        } catch (PackageManager.NameNotFoundException e) {
            return false;
        }
    }

    public static void OpenFile(Activity activity, String path, String mimeType) {
        Uri uri = Uri.parse(path);
        Intent intent = new Intent(Intent.ACTION_VIEW);
        intent.setDataAndType(uri, mimeType);
        activity.startActivity(intent);
    }

    public static void OpenApp(Activity activity, String pakageName) {
    }

    public static void GoToAllFilesAccess(Activity activity) {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.R && !canManageMedia()) {
            Intent intent = new Intent(Settings.ACTION_MANAGE_APP_ALL_FILES_ACCESS_PERMISSION);
            intent.setData(Uri.fromParts("package", activity.getPackageName(), null));
            intent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
            activity.startActivity(intent);
        }
    }

    public static boolean canManageMedia() {
        return android.os.Environment.isExternalStorageManager();
    }

    public static void ShowToast(Activity activity, String msg) {
        android.widget.Toast.makeText(activity, msg, 0);
    }

    public static int CheckPermission(Activity activity, String permission) {
        return activity.checkSelfPermission(permission);
    }

    public static void RequestPermission(Activity activity, String permission) {
        activity.requestPermissions(new String[] { permission }, 0);
    }

    public static void ShowAlert(Context context, String title, String message) {
        AlertDialog.Builder builder = new Builder(context);
        builder.setTitle(title);
        builder.setMessage(message);
        builder.setPositiveButton("Ok", null);
        builder.show();
    }

}