#import <Foundation/Foundation.h>
extern "C" void LighFightEmit(const char *json) {
    NSString *payload = json ? [NSString stringWithUTF8String:json] : @"";
    dispatch_async(dispatch_get_main_queue(), ^{
        [[NSNotificationCenter defaultCenter] postNotificationName:@"LighFightEvent" object:payload];
    });
}
