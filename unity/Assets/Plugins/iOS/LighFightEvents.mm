#import <Foundation/Foundation.h>
extern "C" void LighFightEmit(const char *json) {
    NSString *payload = json ? [NSString stringWithUTF8String:json] : @"";
    dispatch_async(dispatch_get_main_queue(), ^{
        [[NSNotificationCenter defaultCenter] postNotificationName:@"LighFightEvent" object:payload];
    });
}

extern "C" long long LighFightAvailableBytes() {
    NSError *error = nil;
    NSDictionary *attributes = [[NSFileManager defaultManager] attributesOfFileSystemForPath:NSHomeDirectory() error:&error];
    return attributes ? [attributes[NSFileSystemFreeSize] longLongValue] : -1;
}
